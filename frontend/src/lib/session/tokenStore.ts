/**
 * Holds the current JWT access token in memory only — never localStorage/sessionStorage, so it
 * isn't readable by an injected script the way storage is. It's lost on every full page reload by
 * design; AuthContext restores it on mount via the HttpOnly refresh cookie
 * (POST /api/v1/auth/refresh), which JS can't read but the browser sends automatically.
 *
 * apiClient reads this to attach `Authorization: Bearer <token>` to every request, and reports a
 * 401 back through `notifyUnauthorized` so AuthContext can clear state and the app can redirect to
 * /login — without apiClient needing to know anything about React or routing.
 */

let accessToken: string | null = null;
type UnauthorizedListener = () => void;
const unauthorizedListeners = new Set<UnauthorizedListener>();

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function onUnauthorized(listener: UnauthorizedListener): () => void {
  unauthorizedListeners.add(listener);
  return () => unauthorizedListeners.delete(listener);
}

export function notifyUnauthorized(): void {
  unauthorizedListeners.forEach((l) => l());
}
