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

    [HttpGet("ls")]
    public async Task<IActionResult> ListLoadSurvey(
        [FromQuery] Guid? meterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);

        var query = _db.LoadSurveyIntervals.AsQueryable();
        if (meterId.HasValue) query = query.Where(i => i.MeterId == meterId.Value);
        if (fromDate.HasValue) query = query.Where(i => i.IntervalStartUtc >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.IntervalStartUtc <= toDate.Value);
        query = query.OrderByDescending(i => i.IntervalEndUtc);

        var total = await query.CountAsync(ct);

        if (export == "csv")
        {
            var all = await query.ToListAsync(ct);
            var csv = CsvWriter.Write(
                ["Meter Id", "Interval Start (UTC)", "Interval End (UTC)", "Cumulative Reading", "Consumption (kWh)", "Quality", "Source"],
                all, i => [i.MeterId.ToString(), i.IntervalStartUtc, i.IntervalEndUtc, i.CumulativeReading, i.ConsumptionKwh, i.Quality.ToString(), i.Source.ToString()]);
            return File(csv, "text/csv", $"MDMS_LoadSurvey_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var rows = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new ListResult<LoadSurveyInterval>(rows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
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

    [HttpGet("dlp")]
    public async Task<IActionResult> ListDailyLoadProfiles(
        [FromQuery] Guid? meterId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);

        var query = _db.DailyLoadProfiles.AsQueryable();
        if (meterId.HasValue) query = query.Where(i => i.MeterId == meterId.Value);
        if (fromDate.HasValue) query = query.Where(i => i.ProfileDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.ProfileDate <= toDate.Value);
        query = query.OrderByDescending(i => i.ProfileDate);

        var total = await query.CountAsync(ct);

        if (export == "csv")
        {
            var all = await query.ToListAsync(ct);
            var csv = CsvWriter.Write(
                ["Meter Id", "Profile Date", "kWh Import", "kVAh Import", "kWh Export", "kVAh Export", "Quality", "Source"],
                all, i => [i.MeterId.ToString(), i.ProfileDate.ToString("yyyy-MM-dd"), i.ConsumptionKwh, i.KvahImport, i.KwhExport, i.KvahExport, i.Quality.ToString(), i.Source.ToString()]);
            return File(csv, "text/csv", $"MDMS_DailyProfile_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var rows = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new ListResult<DailyLoadProfile>(rows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
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

    [HttpGet("ip")]
    public async Task<IActionResult> ListInstantaneousProfiles(
        [FromQuery] Guid? meterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);

        var query = _db.InstantaneousProfiles.AsQueryable();
        if (meterId.HasValue) query = query.Where(i => i.MeterId == meterId.Value);
        if (fromDate.HasValue) query = query.Where(i => i.MeterTimeUtc >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.MeterTimeUtc <= toDate.Value);
        query = query.OrderByDescending(i => i.MeterTimeUtc);

        var total = await query.CountAsync(ct);

        if (export == "csv")
        {
            var all = await query.ToListAsync(ct);
            var csv = CsvWriter.Write(
                ["Meter Id", "Meter Time (UTC)", "Voltage", "Phase Current", "Neutral Current", "Power Factor", "Frequency",
                 "kW", "kVA", "kWh", "kVAh", "kWh Export", "kVAh Export",
                 "MD kW", "MD kW At", "MD kVA", "MD kVA At", "MD kW Export", "MD kW Export At", "MD kVA Export", "MD kVA Export At",
                 "Power On Duration (min)", "Tamper Count", "Billing Count", "Programming Count", "Load Limit State", "Load Limit Value"],
                all, i => [
                    i.MeterId.ToString(), i.MeterTimeUtc, i.Voltage, i.PhaseCurrent, i.NeutralCurrent, i.PowerFactor, i.Frequency,
                    i.Kw, i.Kva, i.Kwh, i.Kvah, i.KwhExport, i.KvahExport,
                    i.MdKw, i.MdKwAtUtc, i.MdKva, i.MdKvaAtUtc, i.MdKwExport, i.MdKwExportAtUtc, i.MdKvaExport, i.MdKvaExportAtUtc,
                    i.PowerOnDurationMinutes, i.TamperCount, i.BillingCount, i.ProgrammingCount, i.LoadLimitState.ToString(), i.LoadLimitValue]);
            return File(csv, "text/csv", $"MDMS_InstantaneousProfile_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var rows = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new ListResult<InstantaneousProfile>(rows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
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

    [HttpGet("bp")]
    public async Task<IActionResult> ListBillingProfiles(
        [FromQuery] Guid? meterId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);

        var query = _db.BillingProfiles.AsQueryable();
        if (meterId.HasValue) query = query.Where(i => i.MeterId == meterId.Value);
        if (fromDate.HasValue) query = query.Where(i => i.BillingDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.BillingDate <= toDate.Value);
        query = query.OrderByDescending(i => i.BillingDate);

        var total = await query.CountAsync(ct);

        if (export == "csv")
        {
            var all = await query.ToListAsync(ct);
            var headers = new List<string>
            {
                "Meter Id", "Billing Date",
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
                    i.MeterId.ToString(), i.BillingDate.ToString("yyyy-MM-dd"),
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

        var rows = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new ListResult<BillingProfile>(rows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }

    // --------------------------------------------------------------------------------- Events

    public record IngestMeterEventRequest(Guid MeterId, DateTime OccurredAtUtc, MeterEventType EventType, MeterEventSeverity Severity, string? Description);

    /// <summary>Shared ingest for both Events and Alarms — which bucket a row lands in is
    /// determined entirely by its own Severity, not by which endpoint ingested it.</summary>
    [HttpPost("events")]
    public async Task<IActionResult> IngestMeterEvents([FromBody] IReadOnlyList<IngestMeterEventRequest> requests, CancellationToken ct)
    {
        if (requests.Count == 0)
            return BadRequest("At least one event is required.");

        var events = requests.Select(r => new MeterEvent(r.MeterId, r.OccurredAtUtc, r.EventType, r.Severity, r.Description)).ToList();
        _db.MeterEvents.AddRange(events);
        await _db.SaveChangesAsync(ct);
        return Ok(events.Select(e => new { e.Id, e.MeterId, e.OccurredAtUtc, e.EventType, e.Severity }));
    }

    private async Task<IActionResult> ListMeterEventsBySeverity(
        MeterEventSeverity[] severities, string filenamePrefix,
        Guid? meterId, DateTime? fromDate, DateTime? toDate, bool? acknowledged,
        int? page, int? pageSize, string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);

        var query = _db.MeterEvents.Where(e => severities.Contains(e.Severity));
        if (meterId.HasValue) query = query.Where(e => e.MeterId == meterId.Value);
        if (fromDate.HasValue) query = query.Where(e => e.OccurredAtUtc >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(e => e.OccurredAtUtc <= toDate.Value);
        if (acknowledged.HasValue) query = query.Where(e => e.IsAcknowledged == acknowledged.Value);
        query = query.OrderByDescending(e => e.OccurredAtUtc);

        var total = await query.CountAsync(ct);

        if (export == "csv")
        {
            var all = await query.ToListAsync(ct);
            var csv = CsvWriter.Write(
                ["Meter Id", "Occurred (UTC)", "Type", "Severity", "Description", "Acknowledged", "Acknowledged At (UTC)"],
                all, e => [e.MeterId.ToString(), e.OccurredAtUtc, e.EventType.ToString(), e.Severity.ToString(), e.Description, e.IsAcknowledged, e.AcknowledgedAtUtc]);
            return File(csv, "text/csv", $"MDMS_{filenamePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var rows = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new ListResult<MeterEvent>(rows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }

    /// <summary>Info-severity rows only — a "real event", not an alarm condition. See
    /// <see cref="ListAlarms"/> for the Warning/Critical counterpart.</summary>
    [HttpGet("events")]
    public Task<IActionResult> ListEvents(
        [FromQuery] Guid? meterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] bool? acknowledged,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
        => ListMeterEventsBySeverity([MeterEventSeverity.Info], "Events", meterId, fromDate, toDate, acknowledged, page, pageSize, export, ct);

    // --------------------------------------------------------------------------------- Alarms

    /// <summary>Warning/Critical-severity rows only. A separate screen from Events per this
    /// project's own reference UI, even though both read the same underlying table.</summary>
    [HttpGet("alarms")]
    public Task<IActionResult> ListAlarms(
        [FromQuery] Guid? meterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] bool? acknowledged,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
        => ListMeterEventsBySeverity([MeterEventSeverity.Warning, MeterEventSeverity.Critical], "Alarms", meterId, fromDate, toDate, acknowledged, page, pageSize, export, ct);

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
        return Ok(meterEvent);
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
