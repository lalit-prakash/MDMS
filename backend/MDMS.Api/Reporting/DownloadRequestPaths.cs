namespace MDMS.Api.Reporting;

/// <summary>Which API paths a Download Request may export. The worker replays the path against
/// this API as the requesting user, so it is restricted to the known CSV-capable list endpoints,
/// never an arbitrary URL.</summary>
public static class DownloadRequestPaths
{
    private static readonly string[] Allowed =
    [
        "/api/v1/meter-data/", "/api/v1/network/", "/api/v1/customers/master", "/api/v1/reports/",
    ];

    public static bool IsAllowed(string path) =>
        path.StartsWith('/') && !path.Contains("//") && !path.Contains("..") && !path.Contains('#')
        && Allowed.Any(a => path.StartsWith(a, StringComparison.OrdinalIgnoreCase));

    /// <summary>Strips page/pageSize/export from a query string so the worker exports every row.</summary>
    public static string Normalize(string path)
    {
        var i = path.IndexOf('?');
        if (i < 0) return path;
        var kept = path[(i + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(kv => !kv.StartsWith("page=", StringComparison.OrdinalIgnoreCase)
                      && !kv.StartsWith("pageSize=", StringComparison.OrdinalIgnoreCase)
                      && !kv.StartsWith("export=", StringComparison.OrdinalIgnoreCase));
        var q = string.Join('&', kept);
        return q.Length == 0 ? path[..i] : $"{path[..i]}?{q}";
    }
}
