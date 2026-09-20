using MDMS.Api.Reporting;
using MDMS.Application.Common;
using MDMS.Application.MeterData;
using MDMS.Application.Reporting;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Measurement ingestion and retrieval — every category this project's meter-data spec defines:
/// Load Survey (LS, 30-min interval energy), Daily Load Profile (DLP/DP, daily energy),
/// Instantaneous Profile (IP, 15-min point-in-time electrical state), Billing Profile (BP, monthly
/// commercial snapshot), meter Events, meter Alarms, and data-quality holds.
///
/// Every list endpoint here follows the same shape: max 100 rows/page (ReportPaging), an optional
/// from/to date range on the record's own timestamp, an optional meterId filter, and
/// <c>?export=csv</c> to download every matching row (not just the current page) as CSV — the
/// same pattern ReportsController uses, applied to raw meter data rather than business reports.
/// Events and Alarms are deliberately separate endpoints, even though both read the same
/// MeterEvent table filtered by severity (Alarm = Warning/Critical, Event = Info) — matching how
/// this project's own reference UI treats them as two distinct screens, not a shared one.
/// </summary>
[ApiController]
[Route("api/v1/meter-data")]
public class MeterDataController : ControllerBase
{
    private readonly IMdmsDbContext _db;
    private readonly LoadSurveyIngestionService _ingestionService;
    private readonly DailyLoadProfileIngestionService _dlpIngestionService;

    public MeterDataController(
        IMdmsDbContext db,
        LoadSurveyIngestionService ingestionService,
        DailyLoadProfileIngestionService dlpIngestionService)
    {
        _db = db;
        _ingestionService = ingestionService;
        _dlpIngestionService = dlpIngestionService;
    }

    private static (int page, int pageSize) Page(int? page, int? pageSize) => ReportPaging.Normalize(page, pageSize);

    /// <summary>Looks up MeterId → SerialNumber ("Meter Number") for a batch of rows in one query —
    /// every meter-data list endpoint surfaces the human-readable Meter Number, never just the
    /// internal MeterId GUID, per the reference UI.</summary>
    private async Task<Dictionary<Guid, string>> MeterNumbersAsync(IEnumerable<Guid> meterIds, CancellationToken ct)
    {
        var ids = meterIds.Distinct().ToList();
        return await _db.Meters
            .Where(m => ids.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.SerialNumber, ct);
    }

    /// <summary>
    /// Resolves Feeder/DTR/Region-Zone-Circle-Division-SubDivision filters down to the concrete
    /// set of MeterIds they cover, by walking Meter → (active) MeterAssignment → ServicePoint →
    /// DT node → Feeder node → Substation node → OrgUnit, the same real hierarchy
    /// <see cref="NetworkController"/> and <see cref="CustomersController"/> resolve — never a
    /// fabricated mapping. Returns null when no hierarchy filter was requested (meaning: don't
    /// restrict by meter at all), so callers can tell "no filter" from "filter matched nothing".
    /// </summary>
    private async Task<HashSet<Guid>?> ResolveHierarchyMeterIdsAsync(
        Guid? orgUnitId, Guid? feederNodeId, Guid? dtNodeId, CancellationToken ct)
    {
        if (orgUnitId is null && feederNodeId is null && dtNodeId is null)
            return null;

        var nodes = await _db.HierarchyNodes.ToListAsync(ct);
        var nodesById = nodes.ToDictionary(n => n.Id);
        var resolveChain = OrgUnitChainResolver.Build(await _db.OrgUnits.ToListAsync(ct));

        bool NodeMatches(HierarchyNode? dt)
        {
            if (dt is null) return false;
            if (dtNodeId.HasValue && dt.Id != dtNodeId) return false;

            var feeder = dt.ParentId.HasValue && nodesById.TryGetValue(dt.ParentId.Value, out var f) ? f : null;
            if (feederNodeId.HasValue && feeder?.Id != feederNodeId) return false;

            if (orgUnitId.HasValue)
            {
                var substation = feeder?.ParentId.HasValue == true && nodesById.TryGetValue(feeder.ParentId!.Value, out var s) ? s : null;
                if (!resolveChain(substation?.OrgUnitId).Contains(orgUnitId.Value)) return false;
            }

            return true;
        }

        var matchingDtIds = nodes
            .Where(n => n.NodeType == HierarchyNodeType.DistributionTransformer && NodeMatches(n))
            .Select(n => n.Id)
            .ToHashSet();

        var servicePointIds = await _db.ServicePoints
            .Where(sp => sp.DistributionTransformerNodeId.HasValue && matchingDtIds.Contains(sp.DistributionTransformerNodeId.Value))
            .Select(sp => sp.Id)
            .ToListAsync(ct);

        var meterIds = await _db.MeterAssignments
            .Where(a => servicePointIds.Contains(a.ServicePointId) && a.EffectiveToUtc == null)
            .Select(a => a.MeterId)
            .ToListAsync(ct);

        return meterIds.ToHashSet();
    }

    // --------------------------------------------------------------------------- Load Survey (LS)

    [HttpPost("ls")]
    public async Task<IActionResult> IngestLoadSurvey(
        [FromBody] IReadOnlyList<LoadSurveyIngestRequest> requests, CancellationToken ct)
    {
        if (requests.Count == 0)
            return BadRequest("At least one interval is required.");

        var results = await _ingestionService.IngestAsync(requests, ct);
        return Ok(results);
    }

    /// <summary>LS row exposed to callers: the human-readable Meter Number (not the internal
    /// MeterId GUID) plus every LS/BLP parameter from the meter-data spec — Meter Timestamp is the
    /// interval's own end time, MDM Entry Timestamp is when MDMS itself persisted the row.</summary>
    public record LoadSurveyRow(
        Guid Id, Guid MeterId, string MeterNumber,
        DateTime IntervalStartUtc, DateTime IntervalEndUtc, DateTime MdmEntryTimestampUtc,
        decimal CumulativeReading, decimal ConsumptionKwh,
        decimal? AverageVoltage, decimal? AverageCurrent,
        decimal? CumulativeKvahImport, decimal? CumulativeKwhExport, decimal? CumulativeKvahExport,
        string Quality, string Source);

    private static LoadSurveyRow ToRow(LoadSurveyInterval i, IReadOnlyDictionary<Guid, string> meterNumbers) => new(
        i.Id, i.MeterId, meterNumbers.GetValueOrDefault(i.MeterId, i.MeterId.ToString()),
        i.IntervalStartUtc, i.IntervalEndUtc, i.CreatedAtUtc,
        i.CumulativeReading, i.ConsumptionKwh,
        i.AverageVoltage, i.AverageCurrent, i.CumulativeKvahImport, i.CumulativeKwhExport, i.CumulativeKvahExport,
        i.Quality.ToString(), i.Source.ToString());

    [HttpGet("ls")]
    public async Task<IActionResult> ListLoadSurvey(
        [FromQuery] Guid? meterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] Guid? orgUnitId, [FromQuery] Guid? feederNodeId, [FromQuery] Guid? dtNodeId,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);
        var hierarchyMeterIds = await ResolveHierarchyMeterIdsAsync(orgUnitId, feederNodeId, dtNodeId, ct);

        var query = _db.LoadSurveyIntervals.AsQueryable();
        if (meterId.HasValue) query = query.Where(i => i.MeterId == meterId.Value);
        if (hierarchyMeterIds is not null) query = query.Where(i => hierarchyMeterIds.Contains(i.MeterId));
        if (fromDate.HasValue) query = query.Where(i => i.IntervalStartUtc >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.IntervalStartUtc <= toDate.Value);
        query = query.OrderByDescending(i => i.IntervalEndUtc);

        var total = await query.CountAsync(ct);

        if (export == "csv")
        {
            var all = await query.ToListAsync(ct);
            var meterNumbers = await MeterNumbersAsync(all.Select(i => i.MeterId), ct);
            var rowsForCsv = all.Select(i => ToRow(i, meterNumbers)).ToList();
            var csv = CsvWriter.Write(
                ["Meter Number", "Meter Timestamp (UTC)", "MDM Entry Timestamp (UTC)", "Average Voltage", "Average Current",
                 "Cumulative Energy kWh Import", "Cumulative Energy kVAh Import", "Cumulative Energy kWh Export", "Cumulative Energy kVAh Export",
                 "Consumption (kWh)", "Quality", "Source"],
                rowsForCsv, r => [r.MeterNumber, r.IntervalEndUtc, r.MdmEntryTimestampUtc, r.AverageVoltage, r.AverageCurrent,
                    r.CumulativeReading, r.CumulativeKvahImport, r.CumulativeKwhExport, r.CumulativeKvahExport,
                    r.ConsumptionKwh, r.Quality, r.Source]);
            return File(csv, "text/csv", $"MDMS_LoadSurvey_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);
        var pageMeterNumbers = await MeterNumbersAsync(pageRows.Select(i => i.MeterId), ct);
        var rows = pageRows.Select(i => ToRow(i, pageMeterNumbers)).ToList();
        return Ok(new ListResult<LoadSurveyRow>(rows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }

    // ------------------------------------------------------------------------- Daily Profile (DP)

    [HttpPost("dlp")]
    public async Task<IActionResult> IngestDailyLoadProfile(
        [FromBody] DailyLoadProfileIngestRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _dlpIngestionService.IngestAsync(request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            // A received DLP already exists for this meter/date — never silently overwritten.
            return Conflict(ex.Message);
        }
    }

    public record DailyLoadProfileRow(
        Guid Id, Guid MeterId, string MeterNumber, DateOnly ProfileDate, DateTime MdmEntryTimestampUtc,
        decimal ConsumptionKwh, decimal? KvahImport, decimal? KwhExport, decimal? KvahExport,
        string Quality, string Source);

    private static DailyLoadProfileRow ToRow(DailyLoadProfile i, IReadOnlyDictionary<Guid, string> meterNumbers) => new(
        i.Id, i.MeterId, meterNumbers.GetValueOrDefault(i.MeterId, i.MeterId.ToString()), i.ProfileDate, i.CreatedAtUtc,
        i.ConsumptionKwh, i.KvahImport, i.KwhExport, i.KvahExport, i.Quality.ToString(), i.Source.ToString());

    [HttpGet("dlp")]
    public async Task<IActionResult> ListDailyLoadProfiles(
        [FromQuery] Guid? meterId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? orgUnitId, [FromQuery] Guid? feederNodeId, [FromQuery] Guid? dtNodeId,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);
        var hierarchyMeterIds = await ResolveHierarchyMeterIdsAsync(orgUnitId, feederNodeId, dtNodeId, ct);

        var query = _db.DailyLoadProfiles.AsQueryable();
        if (meterId.HasValue) query = query.Where(i => i.MeterId == meterId.Value);
        if (hierarchyMeterIds is not null) query = query.Where(i => hierarchyMeterIds.Contains(i.MeterId));
        if (fromDate.HasValue) query = query.Where(i => i.ProfileDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.ProfileDate <= toDate.Value);
        query = query.OrderByDescending(i => i.ProfileDate);

        var total = await query.CountAsync(ct);

        if (export == "csv")
        {
            var all = await query.ToListAsync(ct);
            var meterNumbers = await MeterNumbersAsync(all.Select(i => i.MeterId), ct);
            var rowsForCsv = all.Select(i => ToRow(i, meterNumbers)).ToList();
            var csv = CsvWriter.Write(
                ["Meter Number", "Profile Date", "MDM Entry Timestamp (UTC)", "kWh Import", "kVAh Import", "kWh Export", "kVAh Export", "Quality", "Source"],
                rowsForCsv, r => [r.MeterNumber, r.ProfileDate.ToString("yyyy-MM-dd"), r.MdmEntryTimestampUtc, r.ConsumptionKwh, r.KvahImport, r.KwhExport, r.KvahExport, r.Quality, r.Source]);
            return File(csv, "text/csv", $"MDMS_DailyProfile_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);
        var pageMeterNumbers = await MeterNumbersAsync(pageRows.Select(i => i.MeterId), ct);
        var rows = pageRows.Select(i => ToRow(i, pageMeterNumbers)).ToList();
        return Ok(new ListResult<DailyLoadProfileRow>(rows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }

    // ------------------------------------------------------------------ Instantaneous Profile (IP)

    public record IngestInstantaneousProfileRequest(
        Guid MeterId, DateTime MeterTimeUtc,
        decimal Voltage, decimal PhaseCurrent, decimal NeutralCurrent, decimal PowerFactor, decimal Frequency,
        decimal Kw, decimal Kva, decimal Kwh, decimal Kvah, decimal KwhExport, decimal KvahExport,
        int PowerOnDurationMinutes, int TamperCount, int BillingCount, int ProgrammingCount,
        LoadLimitState LoadLimitState, decimal? LoadLimitValue,
        decimal? MdKw = null, DateTime? MdKwAtUtc = null,
        decimal? MdKva = null, DateTime? MdKvaAtUtc = null,
        decimal? MdKwExport = null, DateTime? MdKwExportAtUtc = null,
        decimal? MdKvaExport = null, DateTime? MdKvaExportAtUtc = null);

    /// <summary>Batch IP ingest, 15-minute cadence per the meter-data spec. A timestamp already
    /// recorded for that meter is skipped, not overwritten — reported back per-item so a caller
    /// can tell a duplicate from a genuine failure.</summary>
    [HttpPost("ip")]
    public async Task<IActionResult> IngestInstantaneousProfile(
        [FromBody] IReadOnlyList<IngestInstantaneousProfileRequest> requests, CancellationToken ct)
    {
        if (requests.Count == 0)
            return BadRequest("At least one reading is required.");

        var results = new List<object>();
        foreach (var r in requests)
        {
            var exists = await _db.InstantaneousProfiles.AnyAsync(p => p.MeterId == r.MeterId && p.MeterTimeUtc == r.MeterTimeUtc, ct);
            if (exists)
            {
                results.Add(new { r.MeterId, r.MeterTimeUtc, Status = "Skipped-Duplicate" });
                continue;
            }

            var profile = new InstantaneousProfile(
                r.MeterId, r.MeterTimeUtc, r.Voltage, r.PhaseCurrent, r.NeutralCurrent, r.PowerFactor, r.Frequency,
                r.Kw, r.Kva, r.Kwh, r.Kvah, r.KwhExport, r.KvahExport,
                r.PowerOnDurationMinutes, r.TamperCount, r.BillingCount, r.ProgrammingCount,
                r.LoadLimitState, r.LoadLimitValue,
                r.MdKw, r.MdKwAtUtc, r.MdKva, r.MdKvaAtUtc, r.MdKwExport, r.MdKwExportAtUtc, r.MdKvaExport, r.MdKvaExportAtUtc);

            _db.InstantaneousProfiles.Add(profile);
            results.Add(new { r.MeterId, r.MeterTimeUtc, Status = "Ingested", profile.Id });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(results);
    }

    public record InstantaneousProfileRow(
        Guid Id, Guid MeterId, string MeterNumber, DateTime MeterTimeUtc,
        decimal Voltage, decimal PhaseCurrent, decimal NeutralCurrent, decimal PowerFactor, decimal Frequency,
        decimal Kw, decimal Kva, decimal Kwh, decimal Kvah, decimal KwhExport, decimal KvahExport,
        decimal? MdKw, DateTime? MdKwAtUtc, decimal? MdKva, DateTime? MdKvaAtUtc,
        decimal? MdKwExport, DateTime? MdKwExportAtUtc, decimal? MdKvaExport, DateTime? MdKvaExportAtUtc,
        int PowerOnDurationMinutes, int TamperCount, int BillingCount, int ProgrammingCount,
        string LoadLimitState, decimal? LoadLimitValue);

    private static InstantaneousProfileRow ToRow(InstantaneousProfile i, IReadOnlyDictionary<Guid, string> meterNumbers) => new(
        i.Id, i.MeterId, meterNumbers.GetValueOrDefault(i.MeterId, i.MeterId.ToString()), i.MeterTimeUtc,
        i.Voltage, i.PhaseCurrent, i.NeutralCurrent, i.PowerFactor, i.Frequency,
        i.Kw, i.Kva, i.Kwh, i.Kvah, i.KwhExport, i.KvahExport,
        i.MdKw, i.MdKwAtUtc, i.MdKva, i.MdKvaAtUtc, i.MdKwExport, i.MdKwExportAtUtc, i.MdKvaExport, i.MdKvaExportAtUtc,
        i.PowerOnDurationMinutes, i.TamperCount, i.BillingCount, i.ProgrammingCount, i.LoadLimitState.ToString(), i.LoadLimitValue);

    [HttpGet("ip")]
    public async Task<IActionResult> ListInstantaneousProfiles(
        [FromQuery] Guid? meterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] Guid? orgUnitId, [FromQuery] Guid? feederNodeId, [FromQuery] Guid? dtNodeId,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);
        var hierarchyMeterIds = await ResolveHierarchyMeterIdsAsync(orgUnitId, feederNodeId, dtNodeId, ct);

        var query = _db.InstantaneousProfiles.AsQueryable();
        if (meterId.HasValue) query = query.Where(i => i.MeterId == meterId.Value);
        if (hierarchyMeterIds is not null) query = query.Where(i => hierarchyMeterIds.Contains(i.MeterId));
        if (fromDate.HasValue) query = query.Where(i => i.MeterTimeUtc >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.MeterTimeUtc <= toDate.Value);
        query = query.OrderByDescending(i => i.MeterTimeUtc);

        var total = await query.CountAsync(ct);

        if (export == "csv")
        {
            var all = await query.ToListAsync(ct);
            var meterNumbers = await MeterNumbersAsync(all.Select(i => i.MeterId), ct);
            var csv = CsvWriter.Write(
                ["Meter Number", "Meter Time (UTC)", "Voltage", "Phase Current", "Neutral Current", "Power Factor", "Frequency",
                 "kW", "kVA", "kWh", "kVAh", "kWh Export", "kVAh Export",
                 "MD kW", "MD kW At", "MD kVA", "MD kVA At", "MD kW Export", "MD kW Export At", "MD kVA Export", "MD kVA Export At",
                 "Power On Duration (min)", "Tamper Count", "Billing Count", "Programming Count", "Load Limit State", "Load Limit Value"],
                all, i => [
                    meterNumbers.GetValueOrDefault(i.MeterId, i.MeterId.ToString()), i.MeterTimeUtc, i.Voltage, i.PhaseCurrent, i.NeutralCurrent, i.PowerFactor, i.Frequency,
                    i.Kw, i.Kva, i.Kwh, i.Kvah, i.KwhExport, i.KvahExport,
                    i.MdKw, i.MdKwAtUtc, i.MdKva, i.MdKvaAtUtc, i.MdKwExport, i.MdKwExportAtUtc, i.MdKvaExport, i.MdKvaExportAtUtc,
                    i.PowerOnDurationMinutes, i.TamperCount, i.BillingCount, i.ProgrammingCount, i.LoadLimitState.ToString(), i.LoadLimitValue]);
            return File(csv, "text/csv", $"MDMS_InstantaneousProfile_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);
        var pageMeterNumbers = await MeterNumbersAsync(pageRows.Select(i => i.MeterId), ct);
        var rows = pageRows.Select(i => ToRow(i, pageMeterNumbers)).ToList();
        return Ok(new ListResult<InstantaneousProfileRow>(rows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }

    // ------------------------------------------------------------------------ Billing Profile (BP)

    public record IngestBillingProfileRequest(
        Guid MeterId, DateOnly BillingDate,
        decimal CumulativeKwhImport, decimal CumulativeKvahImport, decimal CumulativeKwhExport, decimal CumulativeKvahExport,
        decimal AveragePowerFactor, decimal[] KwhByTariffZone, decimal[] KvahByTariffZone,
        decimal MaximumDemandKw, decimal MaximumDemandKva, int BillingPowerOnDurationMinutes);

    /// <summary>One BP per meter per billing cycle — the meter creates exactly one at month start.
    /// A BP already recorded for that meter/date is never overwritten.</summary>
    [HttpPost("bp")]
    public async Task<IActionResult> IngestBillingProfile([FromBody] IngestBillingProfileRequest r, CancellationToken ct)
    {
        var exists = await _db.BillingProfiles.AnyAsync(p => p.MeterId == r.MeterId && p.BillingDate == r.BillingDate, ct);
        if (exists)
            return Conflict($"A Billing Profile already exists for meter {r.MeterId} on {r.BillingDate:O} and is never overwritten.");

        BillingProfile profile;
        try
        {
            profile = new BillingProfile(
                r.MeterId, r.BillingDate, r.CumulativeKwhImport, r.CumulativeKvahImport, r.CumulativeKwhExport, r.CumulativeKvahExport,
                r.AveragePowerFactor, r.KwhByTariffZone, r.KvahByTariffZone, r.MaximumDemandKw, r.MaximumDemandKva, r.BillingPowerOnDurationMinutes);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        _db.BillingProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        return Ok(profile);
    }

    public record BillingProfileRow(
        Guid Id, Guid MeterId, string MeterNumber, DateOnly BillingDate,
        decimal CumulativeKwhImport, decimal CumulativeKvahImport, decimal CumulativeKwhExport, decimal CumulativeKvahExport,
        decimal AveragePowerFactor, decimal[] KwhByTariffZone, decimal[] KvahByTariffZone,
        decimal MaximumDemandKw, decimal MaximumDemandKva, int BillingPowerOnDurationMinutes);

    private static BillingProfileRow ToRow(BillingProfile i, IReadOnlyDictionary<Guid, string> meterNumbers) => new(
        i.Id, i.MeterId, meterNumbers.GetValueOrDefault(i.MeterId, i.MeterId.ToString()), i.BillingDate,
        i.CumulativeKwhImport, i.CumulativeKvahImport, i.CumulativeKwhExport, i.CumulativeKvahExport,
        i.AveragePowerFactor, i.KwhByTariffZone, i.KvahByTariffZone,
        i.MaximumDemandKw, i.MaximumDemandKva, i.BillingPowerOnDurationMinutes);

    [HttpGet("bp")]
    public async Task<IActionResult> ListBillingProfiles(
        [FromQuery] Guid? meterId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? orgUnitId, [FromQuery] Guid? feederNodeId, [FromQuery] Guid? dtNodeId,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);
        var hierarchyMeterIds = await ResolveHierarchyMeterIdsAsync(orgUnitId, feederNodeId, dtNodeId, ct);

        var query = _db.BillingProfiles.AsQueryable();
        if (meterId.HasValue) query = query.Where(i => i.MeterId == meterId.Value);
        if (hierarchyMeterIds is not null) query = query.Where(i => hierarchyMeterIds.Contains(i.MeterId));
        if (fromDate.HasValue) query = query.Where(i => i.BillingDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.BillingDate <= toDate.Value);
        query = query.OrderByDescending(i => i.BillingDate);

        var total = await query.CountAsync(ct);

        if (export == "csv")
        {
            var all = await query.ToListAsync(ct);
            var meterNumbers = await MeterNumbersAsync(all.Select(i => i.MeterId), ct);
            var headers = new List<string>
            {
                "Meter Number", "Billing Date",
                "Cumulative Energy kWh Import (Monthly)", "Cumulative Energy kVAh Import (Monthly)",
                "Cumulative Energy kWh Export (Monthly)", "Cumulative Energy kVAh Export (Monthly)",
                "Average Power Factor",
            };
            headers.AddRange(Enumerable.Range(1, 8).Select(z => $"kWh TZ{z}"));
            headers.AddRange(Enumerable.Range(1, 8).Select(z => $"kVAh TZ{z}"));
            headers.AddRange(["Max Demand kW", "Max Demand kVA", "Billing Power On Duration (min)"]);

            var csv = CsvWriter.Write(headers, all, i =>
            {
                var row = new List<object?>
                {
                    meterNumbers.GetValueOrDefault(i.MeterId, i.MeterId.ToString()), i.BillingDate.ToString("yyyy-MM-dd"),
                    i.CumulativeKwhImport, i.CumulativeKvahImport, i.CumulativeKwhExport, i.CumulativeKvahExport,
                    i.AveragePowerFactor,
                };
                row.AddRange(i.KwhByTariffZone.Cast<object?>());
                row.AddRange(i.KvahByTariffZone.Cast<object?>());
                row.AddRange([i.MaximumDemandKw, i.MaximumDemandKva, i.BillingPowerOnDurationMinutes]);
                return row;
            });
            return File(csv, "text/csv", $"MDMS_BillingProfile_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);
        var pageMeterNumbers = await MeterNumbersAsync(pageRows.Select(i => i.MeterId), ct);
        var rows = pageRows.Select(i => ToRow(i, pageMeterNumbers)).ToList();
        return Ok(new ListResult<BillingProfileRow>(rows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }

    // --------------------------------------------------------------------------------- Events

    public record IngestMeterEventRequest(
        Guid MeterId, DateTime OccurredAtUtc, MeterEventType EventType, MeterEventSeverity Severity, string? Description,
        decimal? OccCurrent = null, decimal? OccVoltage = null, decimal? OccKwh = null, decimal? OccTemperature = null);

    /// <summary>Shared ingest for both Events and Alarms — which bucket a row lands in is
    /// determined entirely by its own Severity, not by which endpoint ingested it.</summary>
    [HttpPost("events")]
    public async Task<IActionResult> IngestMeterEvents([FromBody] IReadOnlyList<IngestMeterEventRequest> requests, CancellationToken ct)
    {
        if (requests.Count == 0)
            return BadRequest("At least one event is required.");

        var events = requests.Select(r => new MeterEvent(
            r.MeterId, r.OccurredAtUtc, r.EventType, r.Severity, r.Description,
            r.OccCurrent, r.OccVoltage, r.OccKwh, r.OccTemperature)).ToList();
        _db.MeterEvents.AddRange(events);
        await _db.SaveChangesAsync(ct);
        return Ok(events.Select(e => new { e.Id, e.MeterId, e.OccurredAtUtc, e.EventType, e.Severity }));
    }

    public record MeterEventRow(
        Guid Id, Guid MeterId, string MeterNumber, DateTime OccurredAtUtc, string EventType, string Classification, string Severity, string? Description,
        decimal? OccCurrent, decimal? OccVoltage, decimal? OccKwh, decimal? OccTemperature,
        DateTime? ResolvedAtUtc, int? DurationMinutes, bool IsAcknowledged, DateTime? AcknowledgedAtUtc);

    private static MeterEventRow ToRow(MeterEvent e, IReadOnlyDictionary<Guid, string>? meterNumbers = null) => new(
        e.Id, e.MeterId, meterNumbers?.GetValueOrDefault(e.MeterId, e.MeterId.ToString()) ?? e.MeterId.ToString(),
        e.OccurredAtUtc, e.EventType.ToString(), MeterEventClassifier.Classify(e.EventType).ToString(), e.Severity.ToString(), e.Description,
        e.OccCurrent, e.OccVoltage, e.OccKwh, e.OccTemperature,
        e.ResolvedAtUtc, e.ResolvedAtUtc.HasValue ? (int)(e.ResolvedAtUtc.Value - e.OccurredAtUtc).TotalMinutes : null,
        e.IsAcknowledged, e.AcknowledgedAtUtc);

    private async Task<IActionResult> ListMeterEventsBySeverity(
        MeterEventSeverity[] severities, string filenamePrefix,
        Guid? meterId, DateTime? fromDate, DateTime? toDate, bool? acknowledged, MeterEventType? eventType,
        Guid? orgUnitId, Guid? feederNodeId, Guid? dtNodeId,
        int? page, int? pageSize, string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);
        var hierarchyMeterIds = await ResolveHierarchyMeterIdsAsync(orgUnitId, feederNodeId, dtNodeId, ct);

        var query = _db.MeterEvents.Where(e => severities.Contains(e.Severity));
        if (meterId.HasValue) query = query.Where(e => e.MeterId == meterId.Value);
        if (hierarchyMeterIds is not null) query = query.Where(e => hierarchyMeterIds.Contains(e.MeterId));
        if (fromDate.HasValue) query = query.Where(e => e.OccurredAtUtc >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(e => e.OccurredAtUtc <= toDate.Value);
        if (acknowledged.HasValue) query = query.Where(e => e.IsAcknowledged == acknowledged.Value);
        if (eventType.HasValue) query = query.Where(e => e.EventType == eventType.Value);
        query = query.OrderByDescending(e => e.OccurredAtUtc);

        var total = await query.CountAsync(ct);

        if (export == "csv")
        {
            var allEntities = await query.ToListAsync(ct);
            var meterNumbers = await MeterNumbersAsync(allEntities.Select(e => e.MeterId), ct);
            var all = allEntities.Select(e => ToRow(e, meterNumbers)).ToList();
            var csv = CsvWriter.Write(
                ["Meter Number", "Occurred (UTC)", "Classification", "Type", "Severity", "Description",
                 "Occ Current", "Occ Voltage", "Occ kWh", "Occ Temp", "Resolved (UTC)", "Duration (min)", "Acknowledged", "Acknowledged At (UTC)"],
                all, e => [e.MeterNumber, e.OccurredAtUtc, e.Classification, e.EventType, e.Severity, e.Description,
                    e.OccCurrent, e.OccVoltage, e.OccKwh, e.OccTemperature, e.ResolvedAtUtc, e.DurationMinutes, e.IsAcknowledged, e.AcknowledgedAtUtc]);
            return File(csv, "text/csv", $"MDMS_{filenamePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageEntities = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);
        var pageMeterNumbers = await MeterNumbersAsync(pageEntities.Select(e => e.MeterId), ct);
        var rows = pageEntities.Select(e => ToRow(e, pageMeterNumbers)).ToList();
        return Ok(new ListResult<MeterEventRow>(rows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }

    /// <summary>One row per (Classification, EventType) with an aggregate count and last
    /// occurrence — the summary view before drilling into individual occurrences. Same
    /// severities/filters as the corresponding list endpoint.</summary>
    private async Task<IActionResult> ClassificationSummary(
        MeterEventSeverity[] severities, Guid? meterId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _db.MeterEvents.Where(e => severities.Contains(e.Severity));
        if (meterId.HasValue) query = query.Where(e => e.MeterId == meterId.Value);
        if (fromDate.HasValue) query = query.Where(e => e.OccurredAtUtc >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(e => e.OccurredAtUtc <= toDate.Value);

        var all = await query.ToListAsync(ct);

        var byType = all
            .GroupBy(e => e.EventType)
            .Select(g => new
            {
                Classification = MeterEventClassifier.Classify(g.Key).ToString(),
                EventType = g.Key.ToString(),
                Count = g.Count(),
                LastOccurrenceUtc = g.Max(e => e.OccurredAtUtc),
            })
            .OrderByDescending(r => r.Count)
            .ToList();

        var byClassification = all
            .GroupBy(e => MeterEventClassifier.Classify(e.EventType))
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        return Ok(new { totalsByClassification = byClassification, summary = byType, generatedAtUtc = DateTime.UtcNow });
    }

    /// <summary>Info-severity rows only — a "real event", not an alarm condition. See
    /// <see cref="ListAlarms"/> for the Warning/Critical counterpart.</summary>
    [HttpGet("events")]
    public Task<IActionResult> ListEvents(
        [FromQuery] Guid? meterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] bool? acknowledged, [FromQuery] MeterEventType? eventType,
        [FromQuery] Guid? orgUnitId, [FromQuery] Guid? feederNodeId, [FromQuery] Guid? dtNodeId,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
        => ListMeterEventsBySeverity([MeterEventSeverity.Info], "Events", meterId, fromDate, toDate, acknowledged, eventType, orgUnitId, feederNodeId, dtNodeId, page, pageSize, export, ct);

    [HttpGet("events/classification-summary")]
    public Task<IActionResult> EventsClassificationSummary(
        [FromQuery] Guid? meterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
        => ClassificationSummary([MeterEventSeverity.Info], meterId, fromDate, toDate, ct);

    // --------------------------------------------------------------------------------- Alarms

    /// <summary>Warning/Critical-severity rows only. A separate screen from Events per this
    /// project's own reference UI, even though both read the same underlying table.</summary>
    [HttpGet("alarms")]
    public Task<IActionResult> ListAlarms(
        [FromQuery] Guid? meterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] bool? acknowledged, [FromQuery] MeterEventType? eventType,
        [FromQuery] Guid? orgUnitId, [FromQuery] Guid? feederNodeId, [FromQuery] Guid? dtNodeId,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
        => ListMeterEventsBySeverity([MeterEventSeverity.Warning, MeterEventSeverity.Critical], "Alarms", meterId, fromDate, toDate, acknowledged, eventType, orgUnitId, feederNodeId, dtNodeId, page, pageSize, export, ct);

    [HttpGet("alarms/classification-summary")]
    public Task<IActionResult> AlarmsClassificationSummary(
        [FromQuery] Guid? meterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
        => ClassificationSummary([MeterEventSeverity.Warning, MeterEventSeverity.Critical], meterId, fromDate, toDate, ct);

    [HttpPost("events/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveMeterEvent(Guid id, CancellationToken ct)
    {
        var meterEvent = await _db.MeterEvents.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (meterEvent is null) return NotFound();

        try
        {
            meterEvent.Resolve(DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ToRow(meterEvent));
    }

    [HttpPost("events/{id:guid}/acknowledge")]
    public async Task<IActionResult> AcknowledgeMeterEvent(Guid id, CancellationToken ct)
    {
        var meterEvent = await _db.MeterEvents.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (meterEvent is null) return NotFound();

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            meterEvent.Acknowledge(userId);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ToRow(meterEvent));
    }

    [HttpGet("billing-holds")]
    public async Task<IActionResult> ListHolds([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _db.DataQualityHolds.AsQueryable();
        if (activeOnly)
            query = query.Where(h => h.IsActive);

        var holds = await query.OrderByDescending(h => h.RaisedAtUtc).ToListAsync(ct);
        return Ok(holds);
    }

    public record ClearHoldRequest(string ResolutionNote);

    [HttpPost("{meterId:guid}/billing-hold/clear")]
    public async Task<IActionResult> ClearHold(Guid meterId, [FromBody] ClearHoldRequest request, CancellationToken ct)
    {
        var hold = await _db.DataQualityHolds
            .Where(h => h.MeterId == meterId && h.IsActive)
            .OrderByDescending(h => h.RaisedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (hold is null)
            return NotFound("No active hold for this meter.");

        hold.Clear(request.ResolutionNote);
        await _db.SaveChangesAsync(ct);

        return Ok(hold);
    }
}
