namespace MDMS.Application.Reporting;

/// <summary>
/// Shared envelope for every report endpoint, per the MDMS reporting spec: a page of rows, the
/// pagination metadata to render "Showing 1-100 of 18,243" and page controls, a summary computed
/// over the FULL filtered population (never just the current page — the spec is explicit that a
/// table and its KPI summary must never show contradictory totals), and a generation timestamp.
/// </summary>
public record PaginationInfo(int Page, int PageSize, int TotalRecords, int TotalPages);

public record ReportResult<TRow, TSummary>(IReadOnlyList<TRow> Data, PaginationInfo Pagination, TSummary Summary, DateTime GeneratedAtUtc);

public static class ReportPaging
{
    /// <summary>Max 100 records per screen — non-negotiable per the reporting spec (rule #1).</summary>
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 25;

    public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
    {
        var p = page is > 0 ? page.Value : 1;
        var size = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;
        return (p, size);
    }

    public static PaginationInfo BuildInfo(int page, int pageSize, int totalRecords)
    {
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);
        return new PaginationInfo(page, pageSize, totalRecords, totalPages);
    }
}
