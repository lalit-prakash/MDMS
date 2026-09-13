"use client";

/**
 * Real authentication: the backend hashes passwords (PBKDF2) and issues a short-lived JWT access
 * token + an HttpOnly refresh-token cookie (see backend/MDMS.Api/Controllers/AuthController.cs).
 * The access token lives ONLY in memory here (via tokenStore) — never localStorage — so an
 * injected script can't read it; the refresh cookie is invisible to JS entirely. On mount, we try
 * a silent refresh to restore the session after a page reload (the cookie survives, the in-memory
 * token doesn't).
 */

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import { getAccessToken, onUnauthorized, setAccessToken } from "./tokenStore";

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5004";

export interface AuthUser {
  id: string;
  username: string;
  displayName: string;
  role: string;
}

interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  user: AuthUser;
}

interface AuthContextValue {
  user: AuthUser | null;
  ready: boolean;
  login: (username: string, password: string) => Promise<{ ok: true } | { ok: false; error: string }>;
  claim: (username: string, password: string) => Promise<{ ok: true } | { ok: false; error: string }>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

async function postAuth(path: string, body?: unknown): Promise<{ ok: true; data: AuthResponse } | { ok: false; error: string }> {
  try {
    const res = await fetch(`${BASE_URL}${path}`, {
      method: "POST",
      credentials: "include", // sends/receives the HttpOnly refresh cookie
      headers: { "Content-Type": "application/json" },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    if (!res.ok) {
      const text = await res.text().catch(() => "");
      return { ok: false, error: text || `Request failed (${res.status}).` };
    }
    return { ok: true, data: (await res.json()) as AuthResponse };
  } catch {
    return { ok: false, error: "Could not reach the auth service — is the backend running?" };
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [ready, setReady] = useState(false);
  const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  // A ref, not a useCallback, deliberately: applySession schedules a timer that calls "the current
  // silent-refresh function," and silentRefresh itself calls applySession — a genuine circular
  // reference between the two. Routing the call through a ref sidesteps the exhaustive-deps
  // ping-pong that circularity causes with useCallback, without changing behavior.
  const silentRefreshRef = useRef<() => Promise<boolean>>(async () => false);

  // Not wrapped in useCallback: neither is part of the exposed context value, so there's no
  // memoization to preserve — each render simply gets a fresh closure over the current setters,
  // which are themselves stable. Simpler than fighting circular useCallback dependencies.
  const applySession = (data: AuthResponse) => {
    setAccessToken(data.accessToken);
    setUser(data.user);

    // Proactively refresh a little before the access token expires, so an idle-but-open tab
    // doesn't suddenly start getting 401s. Failure here just means the next API call's 401 will
    // trigger the reactive sign-out path instead.
    if (refreshTimer.current) clearTimeout(refreshTimer.current);
    const msUntilExpiry = new Date(data.accessTokenExpiresAtUtc).getTime() - Date.now();
    const refreshInMs = Math.max(msUntilExpiry - 60_000, 5_000);
    refreshTimer.current = setTimeout(() => {
      void silentRefreshRef.current();
    }, refreshInMs);
  };

  const clearSession = () => {
    setAccessToken(null);
    setUser(null);
    if (refreshTimer.current) clearTimeout(refreshTimer.current);
  };

  const silentRefresh = useCallback(async () => {
    const result = await postAuth("/api/v1/auth/refresh");
    if (result.ok) {
      applySession(result.data);
    } else {
      clearSession();
    }
    return result.ok;
  }, []);

  useEffect(() => {
    silentRefreshRef.current = silentRefresh;
  }, [silentRefresh]);

  useEffect(() => {
    // One-time restore-from-cookie attempt on mount — SSR has no cookie access and the in-memory
    // token never survives a reload, so this is the only point session state can be reconstructed.
    let cancelled = false;
    async function restore() {
      await silentRefresh();
      if (!cancelled) setReady(true);
    }
    void restore();
    return () => {
      cancelled = true;
    };
  }, [silentRefresh]);

  useEffect(() => onUnauthorized(clearSession), []);

  const login = useCallback(async (username: string, password: string) => {
    const result = await postAuth("/api/v1/auth/login", { username, password });
    if (!result.ok) return { ok: false as const, error: result.error };
    applySession(result.data);
    return { ok: true as const };
  }, []);

  const claim = useCallback(async (username: string, password: string) => {
    const result = await postAuth("/api/v1/auth/claim", { username, password });
    if (!result.ok) return { ok: false as const, error: result.error };
    applySession(result.data);
    return { ok: true as const };
  }, []);

  const logout = useCallback(async () => {
    const token = getAccessToken();
    try {
      await fetch(`${BASE_URL}/api/v1/auth/logout`, {
        method: "POST",
        credentials: "include",
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      });
    } catch {
      // Best-effort — clear local state regardless.
    }
    clearSession();
  }, []);

  const value = useMemo(() => ({ user, ready, login, claim, logout }), [user, ready, login, claim, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
