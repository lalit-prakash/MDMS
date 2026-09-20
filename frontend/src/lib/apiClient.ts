/**
 * Thin fetch wrapper for the MDMS backend API. Every request attaches the current in-memory JWT
 * access token (see lib/session/tokenStore.ts) as `Authorization: Bearer <token>` — the backend
 * requires authentication on every controller endpoint by default. A 401 here doesn't necessarily
 * mean invalid credentials; it usually means the access token expired faster than AuthContext's
 * proactive refresh, so we notify tokenStore's listeners (AuthContext clears session, AppShell's
 * route guard redirects to /login) rather than leaving the page stuck on a silently-failed call.
 */

import { getAccessToken, notifyUnauthorized } from "./session/tokenStore";

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5004";

export class ApiError extends Error {
  constructor(public status: number, public body: string) {
    super(`API request failed (${status}): ${body}`);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getAccessToken();
  const response = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    if (response.status === 401) notifyUnauthorized();
    const body = await response.text().catch(() => "");
    throw new ApiError(response.status, body);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const apiClient = {
  get: <T>(path: string) => request<T>(path, { method: "GET" }),
  delete: <T = void>(path: string) => request<T>(path, { method: "DELETE" }),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "POST", body: body === undefined ? undefined : JSON.stringify(body) }),
};
