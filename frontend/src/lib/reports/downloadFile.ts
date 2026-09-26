import { getAccessToken } from "@/lib/session/tokenStore";
import { tenantHeaders } from "@/lib/session/tenantStore";

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5004";

/** Fetches an authenticated file endpoint as a blob and triggers the browser save. */
export async function downloadFile(path: string, filenameFallback: string): Promise<void> {
  const token = getAccessToken();
  const res = await fetch(`${BASE_URL}${path}`, { headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}), ...tenantHeaders() } });
  if (!res.ok) throw new Error(`Download failed (${res.status})`);

  const blob = await res.blob();
  const match = res.headers.get("Content-Disposition")?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/);
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = match?.[1] ?? filenameFallback;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
