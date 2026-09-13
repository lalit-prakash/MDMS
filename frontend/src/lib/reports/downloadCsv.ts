import { getAccessToken } from "@/lib/session/tokenStore";

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5004";

/**
 * CSV export goes through the same authenticated endpoint as the on-screen data (with
 * `&export=csv` appended), so it always uses exactly the filters the screen currently has applied
 * — per the reporting spec's own rule that an export must never silently use a different filter
 * set than what the user is looking at. A plain `<a href>` can't carry the Authorization header,
 * so this fetches the file as a blob and triggers the save itself.
 */
export async function downloadCsv(pathAndQuery: string, filenameFallback: string): Promise<void> {
  const token = getAccessToken();
  const res = await fetch(`${BASE_URL}${pathAndQuery}${pathAndQuery.includes("?") ? "&" : "?"}export=csv`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
  if (!res.ok) throw new Error(`Export failed (${res.status})`);

  const blob = await res.blob();
  const disposition = res.headers.get("Content-Disposition");
  const match = disposition?.match(/filename="?([^"]+)"?/);
  const filename = match?.[1] ?? filenameFallback;

  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
