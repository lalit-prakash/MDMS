using MDMS.Application.Common;
using MDMS.Application.MeterData;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// VEE (Validation/Estimation/Editing) configuration and checks. Currently covers out-of-range
/// plausibility validation for both Load Survey and Daily Load Profile consumption;
/// negative-consumption checks run inline during ingestion (see
/// <c>MeterDataController.IngestLoadSurvey</c>) rather than here.
/// </summary>
[ApiController]
[Route("api/v1/vee")]
public class VeeController : ControllerBase
{
    private readonly IMdmsDbContext _db;
    private readonly OutOfRangeValidationService _validationService;
    private readonly MissingIntervalEstimationService _estimationService;

    public VeeController(
        IMdmsDbContext db, OutOfRangeValidationService validationService, MissingIntervalEstimationService estimationService)
    {
        _db = db;
        _validationService = validationService;
        _estimationService = estimationService;
    }

    [HttpGet("thresholds")]
    public async Task<IActionResult> ListThresholds([FromQuery] MeasurementRangeType? measurementType, CancellationToken ct)
    {
        var query = _db.MeasurementRangeThresholds.AsQueryable();
        if (measurementType.HasValue)
            query = query.Where(t => t.MeasurementType == measurementType.Value);

        var thresholds = await query
            .OrderBy(t => t.MeasurementType)
            .ThenBy(t => t.MeterId == null ? 0 : 1) // global default first, per type
            .ThenByDescending(t => t.CreatedAtUtc)
            .ToListAsync(ct);

        return Ok(thresholds);
    }

    [HttpPost("thresholds")]
    public async Task<IActionResult> SetThreshold(
        [FromBody] SetMeasurementRangeThresholdRequest request, CancellationToken ct)
    {
        MeasurementRangeThreshold threshold;
        try
        {
            threshold = new MeasurementRangeThreshold(
                request.MeasurementType, request.MeterId, request.MinConsumptionKwh, request.MaxConsumptionKwh);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        _db.MeasurementRangeThresholds.Add(threshold);
        await _db.SaveChangesAsync(ct);

        return Ok(threshold);
    }

    /// <summary>
    /// Re-evaluates persisted Valid Load Survey intervals against their effective threshold and
    /// flags any breach as <c>OutOfRange</c>. Omit <paramref name="meterId"/> to sweep every
    /// meter that has a configured (meter-specific or global) LS threshold.
    /// </summary>
    [HttpPost("out-of-range-checks/ls/run")]
    public async Task<IActionResult> RunLoadSurveyOutOfRangeCheck([FromQuery] Guid? meterId, CancellationToken ct)
    {
        var flagged = await _validationService.RunLoadSurveyCheckAsync(meterId, ct);
        return Ok(new
        {
            flaggedCount = flagged.Count,
            flagged = flagged.Select(i => new
            {
                i.Id,
                i.MeterId,
                i.IntervalStartUtc,
                i.IntervalEndUtc,
                i.ConsumptionKwh,
                i.Quality
            })
        });
    }

    /// <summary>
    /// Re-evaluates persisted Valid Daily Load Profiles against their effective threshold and
    /// flags any breach as <c>OutOfRange</c>. Omit <paramref name="meterId"/> to sweep every
    /// meter that has a configured (meter-specific or global) DLP threshold.
    /// </summary>
    [HttpPost("out-of-range-checks/dlp/run")]
    public async Task<IActionResult> RunDailyLoadProfileOutOfRangeCheck([FromQuery] Guid? meterId, CancellationToken ct)
    {
        var flagged = await _validationService.RunDailyLoadProfileCheckAsync(meterId, ct);
        return Ok(new
        {
            flaggedCount = flagged.Count,
            flagged = flagged.Select(p => new
            {
                p.Id,
                p.MeterId,
                p.ServicePointId,
                p.ProfileDate,
                p.ConsumptionKwh,
                p.Quality
            })
        });
    }

    /// <summary>
    /// Detects the 30-minute LS slots missing for a meter/day (of the 48 expected) without
    /// attempting to estimate anything.
    /// </summary>
    [HttpGet("estimation/ls/missing-slots")]
    public async Task<IActionResult> GetMissingLoadSurveySlots(
        [FromQuery] Guid meterId, [FromQuery] DateOnly date, CancellationToken ct)
    {
        var missing = await _estimationService.DetectMissingSlotsAsync(meterId, date, ct);
        return Ok(missing.Select(s => new { s.Start, s.End }));
    }

    /// <summary>
    /// Estimates every missing LS slot for a meter/day using the "average of surrounding periods"
    /// method, where both immediate neighbors are available — a slot without both stays missing
    /// and is recorded as such, never guessed at or treated as zero. Returns the audit trail for
    /// every slot attempted.
    /// </summary>
    [HttpPost("estimation/ls/run")]
    public async Task<IActionResult> RunLoadSurveyEstimation(
        [FromQuery] Guid meterId, [FromQuery] DateOnly date, CancellationToken ct)
    {
        var records = await _estimationService.EstimateMissingSlotsAsync(meterId, date, ct);
        return Ok(new
        {
            attemptedCount = records.Count,
            estimatedCount = records.Count(r => r.ResultQuality == MeasurementQuality.Valid),
            records = records.Select(r => new
            {
                r.Id,
                r.SlotStartUtc,
                r.SlotEndUtc,
                r.ResultQuality,
                r.NewValue,
                r.Details
            })
        });
    }

    /// <summary>The VEE audit trail — every rule execution recorded, filterable by meter.</summary>
    [HttpGet("execution-records")]
    public async Task<IActionResult> ListExecutionRecords([FromQuery] Guid? meterId, CancellationToken ct)
    {
        var query = _db.VeeExecutionRecords.AsQueryable();
        if (meterId.HasValue)
            query = query.Where(r => r.MeterId == meterId.Value);

        var records = await query.OrderByDescending(r => r.CreatedAtUtc).Take(500).ToListAsync(ct);
        return Ok(records);
    }
}
