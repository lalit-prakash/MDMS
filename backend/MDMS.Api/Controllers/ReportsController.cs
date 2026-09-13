using MDMS.Api.Reporting;
using MDMS.Application.Common;
using MDMS.Application.Reporting;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Shared reporting endpoints, per the MDMS reporting specification: server-side pagination
/// (max 100 rows/page), filtering, a summary computed over the full filtered population (never
/// just the current page — the spec is explicit that a table and its KPI summary must never show
/// contradictory totals), and CSV export using exactly the same filters as the screen
/// (?export=csv on the same GET, rather than a separate background job — our data volumes are
/// small enough that a synchronous export is honest; the spec's async-export guidance is for
/// datasets this deployment doesn't have).
///
/// Every join is done in SQL (via EF) and materialized with ToListAsync BEFORE any enum-based
/// filtering, sorting, or aggregation — EF Core/Npgsql can't reliably translate comparisons
/// against an enum's .ToString() into SQL, and these datasets are small enough that finishing the
/// filter/sort/paginate pipeline in-memory is both correct and fast enough.
///
/// Only reports backed by data this system actually records are implemented here. The full
/// catalogue in the reporting spec includes reports needing data models that don't exist yet
/// (consumer master fields like tariff/sanctioned load, device telemetry, a billing engine,
/// ingestion-pipeline metadata) — those are tracked as follow-up issues, not faked here.
/// </summary>
[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public ReportsController(IMdmsDbContext db) => _db = db;

    private static (int page, int pageSize) Page(int? page, int? pageSize) => ReportPaging.Normalize(page, pageSize);

    // ---------------------------------------------------------------- Meter Inventory (RPT-MTR-001)

    public record MeterInventoryRow(
        Guid MeterId, string SerialNumber, string Phase, string Status,
        string? CustomerAccountNumber, string? CustomerName, string? ServicePointAddress,
        string? DistributionTransformer, DateTime? InstalledAtUtc);

    public record MeterInventorySummary(int Total, int InStock, int Installed, int Removed, int Retired);

    [HttpGet("meter-inventory")]
    public async Task<IActionResult> MeterInventory(
        [FromQuery] string? search, [FromQuery] MeterStatus? status, [FromQuery] MeterPhase? phase,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);

        // Current assignment per meter = the one with no EffectiveToUtc yet (spec 10.1: "one
        // logical current record per meter", not every historical installation row).
        var joined = await (
            from m in _db.Meters
            join a in _db.MeterAssignments.Where(a => a.EffectiveToUtc == null) on m.Id equals a.MeterId into assignments
            from a in assignments.DefaultIfEmpty()
            join sp in _db.ServicePoints on a.ServicePointId equals sp.Id into servicePoints
            from sp in servicePoints.DefaultIfEmpty()
            join c in _db.Customers on sp.CustomerId equals c.Id into customers
            from c in customers.DefaultIfEmpty()
            join dt in _db.HierarchyNodes on sp.DistributionTransformerNodeId equals dt.Id into dts
            from dt in dts.DefaultIfEmpty()
            select new
            {
                m.Id, m.SerialNumber, m.Phase, m.Status,
                CustomerAccountNumber = c != null ? c.AccountNumber : null,
                CustomerName = c != null ? c.Name : null,
                ServicePointAddress = sp != null ? sp.Address : null,
                DistributionTransformer = dt != null ? dt.Name : null,
                InstalledAtUtc = a != null ? (DateTime?)a.EffectiveFromUtc : null,
            }).ToListAsync(ct);

        var all = joined.Select(r => new MeterInventoryRow(
            r.Id, r.SerialNumber, r.Phase.ToString(), r.Status.ToString(),
            r.CustomerAccountNumber, r.CustomerName, r.ServicePointAddress, r.DistributionTransformer, r.InstalledAtUtc));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            all = all.Where(r => r.SerialNumber.Contains(s, StringComparison.OrdinalIgnoreCase)
                || (r.CustomerAccountNumber?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        if (status.HasValue) all = all.Where(r => r.Status == status.Value.ToString());
        if (phase.HasValue) all = all.Where(r => r.Phase == phase.Value.ToString());

        var filtered = all.ToList();
        var total = filtered.Count;
        var summary = new MeterInventorySummary(
            total,
            filtered.Count(r => r.Status == nameof(MeterStatus.InStock)),
            filtered.Count(r => r.Status == nameof(MeterStatus.Installed)),
            filtered.Count(r => r.Status == nameof(MeterStatus.Removed)),
            filtered.Count(r => r.Status == nameof(MeterStatus.Retired)));

        if (export == "csv")
        {
            var csv = CsvWriter.Write(
                ["Meter Number", "Serial Number", "Phase", "Status", "Consumer Number", "Consumer Name", "Service Point", "Distribution Transformer", "Installed At (UTC)"],
                filtered, r => [r.SerialNumber, r.SerialNumber, r.Phase, r.Status, r.CustomerAccountNumber, r.CustomerName, r.ServicePointAddress, r.DistributionTransformer, r.InstalledAtUtc]);
            return File(csv, "text/csv", $"MDMS_Meter_Inventory_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = filtered.Skip((p - 1) * size).Take(size).ToList();
        return Ok(new ReportResult<MeterInventoryRow, MeterInventorySummary>(pageRows, ReportPaging.BuildInfo(p, size, total), summary, DateTime.UtcNow));
    }

    // ---------------------------------------------------------------- VEE Execution (RPT-VEE-001)

    public record VeeExecutionRow(Guid Id, string RuleName, string MeasurementType, string MeterSerialNumber, DateTime SlotStartUtc, DateTime SlotEndUtc, string ResultQuality, decimal? NewValue, string Details);
    public record VeeExecutionSummary(int Total, int Passed, int Failed, double PassRatePercent);

    [HttpGet("vee-execution")]
    public async Task<IActionResult> VeeExecution(
        [FromQuery] string? meterSerialNumber, [FromQuery] MeasurementQuality? resultQuality,
        [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);

        var joinedQuery = from v in _db.VeeExecutionRecords
                           join m in _db.Meters on v.MeterId equals m.Id
                           select new { v.Id, v.RuleName, v.MeasurementType, m.SerialNumber, v.SlotStartUtc, v.SlotEndUtc, v.ResultQuality, v.NewValue, v.Details };

        if (fromUtc.HasValue) joinedQuery = joinedQuery.Where(r => r.SlotStartUtc >= fromUtc.Value);
        if (toUtc.HasValue) joinedQuery = joinedQuery.Where(r => r.SlotStartUtc <= toUtc.Value);

        var joined = await joinedQuery.ToListAsync(ct);

        IEnumerable<VeeExecutionRow> all = joined.Select(r => new VeeExecutionRow(
            r.Id, r.RuleName, r.MeasurementType.ToString(), r.SerialNumber, r.SlotStartUtc, r.SlotEndUtc, r.ResultQuality.ToString(), r.NewValue, r.Details));

        if (!string.IsNullOrWhiteSpace(meterSerialNumber))
            all = all.Where(r => r.MeterSerialNumber.Contains(meterSerialNumber, StringComparison.OrdinalIgnoreCase));
        if (resultQuality.HasValue)
            all = all.Where(r => r.ResultQuality == resultQuality.Value.ToString());

        var filtered = all.OrderByDescending(r => r.SlotStartUtc).ToList();
        var total = filtered.Count;
        var passed = filtered.Count(r => r.ResultQuality == nameof(MeasurementQuality.Valid));
        var summary = new VeeExecutionSummary(total, passed, total - passed, total == 0 ? 0 : Math.Round(passed * 100.0 / total, 1));

        if (export == "csv")
        {
            var csv = CsvWriter.Write(
                ["Meter", "Rule", "Measurement Type", "Slot Start (UTC)", "Slot End (UTC)", "Result Quality", "New Value", "Details"],
                filtered, r => [r.MeterSerialNumber, r.RuleName, r.MeasurementType, r.SlotStartUtc, r.SlotEndUtc, r.ResultQuality, r.NewValue, r.Details]);
            return File(csv, "text/csv", $"MDMS_VEE_Execution_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = filtered.Skip((p - 1) * size).Take(size).ToList();
        return Ok(new ReportResult<VeeExecutionRow, VeeExecutionSummary>(pageRows, ReportPaging.BuildInfo(p, size, total), summary, DateTime.UtcNow));
    }

    // ---------------------------------------------------------------- Complaint Register (RPT-COMP-001)

    public record ComplaintRow(Guid Id, string? ConsumerAccountNumber, string? ConsumerName, string Source, string Description, string Status, DateTime SlaDueUtc, DateTime? ResolvedAtUtc, DateTime? ClosedAtUtc);
    public record ComplaintSummary(int Total, int Open, int Resolved, int Closed, int SlaBreached);

    [HttpGet("complaint-register")]
    public async Task<IActionResult> ComplaintRegister(
        [FromQuery] ComplaintStatus? status, [FromQuery] string? search,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);

        var joined = await (
            from cm in _db.Complaints
            join c in _db.Customers on cm.CustomerId equals c.Id into customers
            from c in customers.DefaultIfEmpty()
            select new
            {
                cm.Id, cm.Source, cm.Description, cm.Status, cm.SlaDueUtc, cm.ResolvedAtUtc, cm.ClosedAtUtc,
                CustomerAccountNumber = c != null ? c.AccountNumber : null,
                CustomerName = c != null ? c.Name : null,
            }).ToListAsync(ct);

        IEnumerable<ComplaintRow> all = joined.Select(r => new ComplaintRow(
            r.Id, r.CustomerAccountNumber, r.CustomerName, r.Source.ToString(), r.Description, r.Status.ToString(), r.SlaDueUtc, r.ResolvedAtUtc, r.ClosedAtUtc));

        if (status.HasValue) all = all.Where(r => r.Status == status.Value.ToString());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            all = all.Where(r => r.Description.Contains(s, StringComparison.OrdinalIgnoreCase)
                || (r.ConsumerAccountNumber?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var filtered = all.OrderByDescending(r => r.SlaDueUtc).ToList();
        var total = filtered.Count;
        var now = DateTime.UtcNow;
        var summary = new ComplaintSummary(
            total,
            filtered.Count(r => r.Status is "Open" or "Assigned" or "InProgress"),
            filtered.Count(r => r.Status == "Resolved"),
            filtered.Count(r => r.Status == "Closed"),
            filtered.Count(r => (r.ClosedAtUtc ?? r.ResolvedAtUtc ?? now) > r.SlaDueUtc));

        if (export == "csv")
        {
            var csv = CsvWriter.Write(
                ["Consumer Number", "Consumer Name", "Source", "Description", "Status", "SLA Due (UTC)", "Resolved At (UTC)", "Closed At (UTC)"],
                filtered, r => [r.ConsumerAccountNumber, r.ConsumerName, r.Source, r.Description, r.Status, r.SlaDueUtc, r.ResolvedAtUtc, r.ClosedAtUtc]);
            return File(csv, "text/csv", $"MDMS_Complaint_Register_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = filtered.Skip((p - 1) * size).Take(size).ToList();
        return Ok(new ReportResult<ComplaintRow, ComplaintSummary>(pageRows, ReportPaging.BuildInfo(p, size, total), summary, DateTime.UtcNow));
    }

    // ---------------------------------------------------------------- Prepaid Balance (RPT-PREPAID-001)

    public record PrepaidBalanceRow(Guid AccountId, string? ConsumerAccountNumber, string? ConsumerName, decimal Balance, bool IsConnected);
    public record PrepaidBalanceSummary(int Total, int Connected, int Disconnected, decimal TotalBalance);

    [HttpGet("prepaid-balance")]
    public async Task<IActionResult> PrepaidBalance(
        [FromQuery] bool? isConnected, [FromQuery] string? search,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);

        var joinedQuery = from a in _db.PrepaidAccounts
                           join c in _db.Customers on a.CustomerId equals c.Id into customers
                           from c in customers.DefaultIfEmpty()
                           select new { a.Id, a.Balance, a.IsConnected, CustomerAccountNumber = c != null ? c.AccountNumber : null, CustomerName = c != null ? c.Name : null };

        if (isConnected.HasValue) joinedQuery = joinedQuery.Where(r => r.IsConnected == isConnected.Value);

        var joined = await joinedQuery.ToListAsync(ct);

        IEnumerable<PrepaidBalanceRow> all = joined.Select(r => new PrepaidBalanceRow(r.Id, r.CustomerAccountNumber, r.CustomerName, r.Balance, r.IsConnected));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            all = all.Where(r => r.ConsumerAccountNumber?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        var filtered = all.OrderBy(r => r.Balance).ToList();
        var total = filtered.Count;
        var summary = new PrepaidBalanceSummary(total, filtered.Count(r => r.IsConnected), filtered.Count(r => !r.IsConnected), filtered.Sum(r => r.Balance));

        if (export == "csv")
        {
            var csv = CsvWriter.Write(
                ["Consumer Number", "Consumer Name", "Balance", "Connected"],
                filtered, r => [r.ConsumerAccountNumber, r.ConsumerName, r.Balance, r.IsConnected]);
            return File(csv, "text/csv", $"MDMS_Prepaid_Balance_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = filtered.Skip((p - 1) * size).Take(size).ToList();
        return Ok(new ReportResult<PrepaidBalanceRow, PrepaidBalanceSummary>(pageRows, ReportPaging.BuildInfo(p, size, total), summary, DateTime.UtcNow));
    }

    // ---------------------------------------------------------------- Revenue Risk (RPT-RP-001)

    public record RevenueRiskRow(Guid Id, string? ConsumerAccountNumber, string? ConsumerName, string? MeterSerialNumber, decimal RiskScore, string Status, decimal? RecoveryAmount);
    public record RevenueRiskSummary(int Total, int Open, int Closed, decimal AverageRiskScore, decimal TotalRecovered);

    [HttpGet("revenue-risk")]
    public async Task<IActionResult> RevenueRisk(
        [FromQuery] RevenueProtectionLeadStatus? status, [FromQuery] decimal? minRiskScore,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);

        var joinedQuery = from l in _db.RevenueProtectionLeads
                           join c in _db.Customers on l.CustomerId equals c.Id into customers
                           from c in customers.DefaultIfEmpty()
                           join m in _db.Meters on l.MeterId equals m.Id into meters
                           from m in meters.DefaultIfEmpty()
                           select new
                           {
                               l.Id, l.RiskScore, l.Status, l.RecoveryAmount,
                               CustomerAccountNumber = c != null ? c.AccountNumber : null,
                               CustomerName = c != null ? c.Name : null,
                               MeterSerialNumber = m != null ? m.SerialNumber : null,
                           };

        if (minRiskScore.HasValue) joinedQuery = joinedQuery.Where(r => r.RiskScore >= minRiskScore.Value);

        var joined = await joinedQuery.ToListAsync(ct);

        IEnumerable<RevenueRiskRow> all = joined.Select(r => new RevenueRiskRow(
            r.Id, r.CustomerAccountNumber, r.CustomerName, r.MeterSerialNumber, r.RiskScore, r.Status.ToString(), r.RecoveryAmount));

        if (status.HasValue) all = all.Where(r => r.Status == status.Value.ToString());

        var filtered = all.OrderByDescending(r => r.RiskScore).ToList();
        var total = filtered.Count;
        var summary = new RevenueRiskSummary(
            total,
            filtered.Count(r => r.Status != "Closed"),
            filtered.Count(r => r.Status == "Closed"),
            total == 0 ? 0 : Math.Round(filtered.Average(r => r.RiskScore), 1),
            filtered.Sum(r => r.RecoveryAmount ?? 0));

        if (export == "csv")
        {
            var csv = CsvWriter.Write(
                ["Consumer Number", "Consumer Name", "Meter", "Risk Score", "Status", "Recovery Amount"],
                filtered, r => [r.ConsumerAccountNumber, r.ConsumerName, r.MeterSerialNumber, r.RiskScore, r.Status, r.RecoveryAmount]);
            return File(csv, "text/csv", $"MDMS_Revenue_Risk_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = filtered.Skip((p - 1) * size).Take(size).ToList();
        return Ok(new ReportResult<RevenueRiskRow, RevenueRiskSummary>(pageRows, ReportPaging.BuildInfo(p, size, total), summary, DateTime.UtcNow));
    }
}
