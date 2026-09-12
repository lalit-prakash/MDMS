using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>Meter master-data operations: registering a meter and its service-point assignment history.</summary>
[ApiController]
[Route("api/v1/meters")]
public class MetersController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public MetersController(IMdmsDbContext db) => _db = db;

    public record CreateMeterRequest(string SerialNumber, MeterPhase Phase);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var meters = await _db.Meters.OrderBy(m => m.SerialNumber).ToListAsync(ct);
        return Ok(meters);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var meter = await _db.Meters.FirstOrDefaultAsync(m => m.Id == id, ct);
        return meter is null ? NotFound() : Ok(meter);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMeterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SerialNumber))
            return BadRequest("SerialNumber is required.");

        var meter = new Meter(request.SerialNumber, request.Phase);
        _db.Meters.Add(meter);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = meter.Id }, meter);
    }

    public record InstallMeterRequest(Guid ServicePointId, DateTime EffectiveFromUtc, decimal? OpeningReading, string? Reason);

    [HttpPost("{id:guid}/install")]
    public async Task<IActionResult> Install(Guid id, [FromBody] InstallMeterRequest request, CancellationToken ct)
    {
        var meter = await _db.Meters.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (meter is null) return NotFound();

        meter.MarkInstalled();
        var assignment = MeterAssignment.CreateInstallation(
            request.ServicePointId, meter.Id, request.EffectiveFromUtc, request.OpeningReading, request.Reason);

        _db.MeterAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);

        return Ok(assignment);
    }

    public record ReplaceMeterRequest(
        Guid ServicePointId, Guid NewMeterId, DateTime EffectiveFromUtc,
        decimal ClosingReadingOfOldMeter, decimal OpeningReadingOfNewMeter, string Reason);

    [HttpPost("{id:guid}/replace")]
    public async Task<IActionResult> Replace(Guid id, [FromBody] ReplaceMeterRequest request, CancellationToken ct)
    {
        var oldMeter = await _db.Meters.FirstOrDefaultAsync(m => m.Id == id, ct);
        var newMeter = await _db.Meters.FirstOrDefaultAsync(m => m.Id == request.NewMeterId, ct);
        if (oldMeter is null || newMeter is null) return NotFound();

        oldMeter.MarkRemoved();
        newMeter.MarkInstalled();

        var assignment = MeterAssignment.CreateReplacement(
            request.ServicePointId, newMeter.Id, request.EffectiveFromUtc,
            request.ClosingReadingOfOldMeter, request.OpeningReadingOfNewMeter, request.Reason);

        _db.MeterAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);

        return Ok(assignment);
    }

    [HttpGet("replacements")]
    public async Task<IActionResult> ListReplacements(CancellationToken ct)
    {
        var replacements = await _db.MeterAssignments
            .Where(a => a.EventType == MeterAssignmentEventType.Replacement)
            .OrderByDescending(a => a.EffectiveFromUtc)
            .ToListAsync(ct);

        return Ok(replacements);
    }
}
