/**
 * The organisation (tenant) the user last switched to, remembered per browser so a reload keeps
 * it. Only a convenience: the server validates every X-Tenant-Id against the user's actual access
 * and falls back to their home organisation, so a stale or tampered value can never widen access.
 */
const KEY = "mdms.activeTenantId";

export function getSelectedTenantId(): string | null {
  try {
    return typeof window === "undefined" ? null : window.localStorage.getItem(KEY);
  } catch {
    return null;
  }
}

export function setSelectedTenantId(id: string | null): void {
  try {
    if (id) window.localStorage.setItem(KEY, id);
    else window.localStorage.removeItem(KEY);
  } catch {
    // storage unavailable: the switcher then simply doesn't persist across reloads
  }
}

/** Headers naming the active organisation, for requests made outside apiClient (file downloads). */
export function tenantHeaders(): Record<string, string> {
  const id = getSelectedTenantId();
  return id ? { "X-Tenant-Id": id } : {};
}
