using MDMS.Application.Common;
using MDMS.Application.MeterData;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Measurement ingestion and retrieval — every category this project's meter-data spec defines:
/// Load Survey (LS, 30-min interval energy), Daily Load Profile (DLP/DP, daily energy),
/// Instantaneous Profile (IP, 15-min point-in-time electrical state), Billing Profile (BP, monthly
/// commercial snapshot), and meter Events/Alarms — plus data-quality holds.
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
    public async Task<IActionResult> ListLoadSurvey([FromQuery] Guid? meterId, CancellationToken ct)
    {
        var query = _db.LoadSurveyIntervals.AsQueryable();
        if (meterId.HasValue)
            query = query.Where(i => i.MeterId == meterId.Value);

        var intervals = await query
            .OrderByDescending(i => i.IntervalEndUtc)
            .Take(500)
            .ToListAsync(ct);

        return Ok(intervals);
    }

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
    public async Task<IActionResult> ListDailyLoadProfiles([FromQuery] Guid? meterId, CancellationToken ct)
    {
        var query = _db.DailyLoadProfiles.AsQueryable();
        if (meterId.HasValue)
            query = query.Where(p => p.MeterId == meterId.Value);

        var profiles = await query
            .OrderByDescending(p => p.ProfileDate)
            .Take(500)
            .ToListAsync(ct);

        return Ok(profiles);
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
    public async Task<IActionResult> ListInstantaneousProfiles([FromQuery] Guid? meterId, CancellationToken ct)
    {
        var query = _db.InstantaneousProfiles.AsQueryable();
        if (meterId.HasValue)
            query = query.Where(p => p.MeterId == meterId.Value);

        var profiles = await query.OrderByDescending(p => p.MeterTimeUtc).Take(500).ToListAsync(ct);
        return Ok(profiles);
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
    public async Task<IActionResult> ListBillingProfiles([FromQuery] Guid? meterId, CancellationToken ct)
    {
        var query = _db.BillingProfiles.AsQueryable();
        if (meterId.HasValue)
            query = query.Where(p => p.MeterId == meterId.Value);

        var profiles = await query.OrderByDescending(p => p.BillingDate).Take(100).ToListAsync(ct);
        return Ok(profiles);
    }

    // ------------------------------------------------------------------------- Events / Alarms

    public record IngestMeterEventRequest(Guid MeterId, DateTime OccurredAtUtc, MeterEventType EventType, MeterEventSeverity Severity, string? Description);

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

    [HttpGet("events")]
    public async Task<IActionResult> ListMeterEvents(
        [FromQuery] Guid? meterId, [FromQuery] MeterEventSeverity? severity, [FromQuery] bool? acknowledged, CancellationToken ct)
    {
        var query = _db.MeterEvents.AsQueryable();
        if (meterId.HasValue) query = query.Where(e => e.MeterId == meterId.Value);
        if (severity.HasValue) query = query.Where(e => e.Severity == severity.Value);
        if (acknowledged.HasValue) query = query.Where(e => e.IsAcknowledged == acknowledged.Value);

        var events = await query.OrderByDescending(e => e.OccurredAtUtc).Take(500).ToListAsync(ct);
        return Ok(events);
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
