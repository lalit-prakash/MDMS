"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";

export type ThemeMode = "light" | "dark";

const STORAGE_KEY = "mdms-theme-mode";

interface ThemeModeContextValue {
  mode: ThemeMode;
  toggleMode: () => void;
}

const ThemeModeContext = createContext<ThemeModeContextValue | null>(null);

/** Reads a persisted choice first, then the OS preference, defaulting to light per the design spec (§41). */
function resolveInitialMode(): ThemeMode {
  if (typeof window === "undefined") return "light";
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    if (stored === "light" || stored === "dark") return stored;
  } catch {
    // localStorage can throw (private browsing, blocked storage) — fall through to system preference.
  }
  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function ThemeModeProvider({ children }: { children: React.ReactNode }) {
  // Light is the correct SSR/first-paint default (spec §41); the effect below reconciles it with
  // any stored/system preference immediately on mount, before the user perceives a flash.
  const [mode, setMode] = useState<ThemeMode>("light");

  useEffect(() => {
    // Intentional one-time read from an external system (localStorage / matchMedia) on mount —
    // exactly the case react-hooks/set-state-in-effect exists to flag, but SSR has no access to
    // either, so "render 'light' on the server, reconcile after mount" is the only hydration-safe
    // way to apply a stored/system preference without a light->dark flash on load.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setMode(resolveInitialMode());
  }, []);

  useEffect(() => {
    document.documentElement.setAttribute("data-theme", mode);
  }, [mode]);

  const toggleMode = useCallback(() => {
    setMode((prev) => {
      const next = prev === "light" ? "dark" : "light";
      try {
        window.localStorage.setItem(STORAGE_KEY, next);
      } catch {
        // Non-fatal — the choice just won't persist across reloads.
      }
      return next;
    });
  }, []);

  const value = useMemo(() => ({ mode, toggleMode }), [mode, toggleMode]);

  return <ThemeModeContext.Provider value={value}>{children}</ThemeModeContext.Provider>;
}

export function useThemeMode() {
  const ctx = useContext(ThemeModeContext);
  if (!ctx) throw new Error("useThemeMode must be used within a ThemeModeProvider");
  return ctx;
}
