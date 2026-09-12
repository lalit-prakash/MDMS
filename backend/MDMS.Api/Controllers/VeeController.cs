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

    public VeeController(IMdmsDbContext db, OutOfRangeValidationService validationService)
    {
        _db = db;
        _validationService = validationService;
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
}
