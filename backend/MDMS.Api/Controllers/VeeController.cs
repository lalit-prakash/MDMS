using MDMS.Application.Common;
using MDMS.Application.MeterData;
using MDMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// VEE (Validation/Estimation/Editing) configuration and checks. Currently covers out-of-range
/// plausibility validation for Load Survey consumption; negative-consumption checks run inline
/// during ingestion (see <c>MeterDataController.IngestLoadSurvey</c>) rather than here.
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
    public async Task<IActionResult> ListThresholds(CancellationToken ct)
    {
        var thresholds = await _db.MeasurementRangeThresholds
            .OrderBy(t => t.MeterId == null ? 0 : 1) // global default first
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
                request.MeterId, request.MinConsumptionKwh, request.MaxConsumptionKwh);
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
    /// meter that has a configured (meter-specific or global) threshold.
    /// </summary>
    [HttpPost("out-of-range-checks/run")]
    public async Task<IActionResult> RunOutOfRangeCheck([FromQuery] Guid? meterId, CancellationToken ct)
    {
        var flagged = await _validationService.RunCheckAsync(meterId, ct);
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
}
