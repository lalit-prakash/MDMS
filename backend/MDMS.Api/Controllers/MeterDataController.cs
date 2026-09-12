using MDMS.Application.Common;
using MDMS.Application.MeterData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>Measurement ingestion and retrieval: Load Survey intervals, and data-quality holds.</summary>
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
