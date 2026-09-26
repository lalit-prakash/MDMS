using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Meter Testing requests — a dedicated module per the reference mobile-app spec's explicit
/// requirement that it not be hidden inside generic complaints. Consumer-facing; scoped by
/// customerId like Complaints.
/// </summary>
[ApiController]
[Route("api/v1/meter-testing")]
public class MeterTestingController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public MeterTestingController(IMdmsDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? customerId, CancellationToken ct)
    {
        var query = _db.MeterTestingRequests.AsQueryable();
        if (customerId.HasValue) query = query.Where(r => r.CustomerId == customerId.Value);
        var rows = await query.OrderByDescending(r => r.CreatedAtUtc).Take(500).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _db.MeterTestingRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        return r is null ? NotFound() : Ok(r);
    }

    public record CreateMeterTestingRequest(Guid CustomerId, Guid MeterId, MeterTestingReason Reason, string? ConsumerRemarks);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMeterTestingRequest request, CancellationToken ct)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists) return NotFound($"Customer {request.CustomerId} not found.");
        var meterExists = await _db.Meters.AnyAsync(m => m.Id == request.MeterId, ct);
        if (!meterExists) return NotFound($"Meter {request.MeterId} not found.");

        var entity = new MeterTestingRequest(request.CustomerId, request.MeterId, request.Reason, request.ConsumerRemarks);
        _db.MeterTestingRequests.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var r = await _db.MeterTestingRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();
        try { r.Cancel(); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        await _db.SaveChangesAsync(ct);
        return Ok(r);
    }

    public record ScheduleRequest(DateTime ScheduledAtUtc);

    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(Guid id, [FromBody] ScheduleRequest request, CancellationToken ct)
    {
        var r = await _db.MeterTestingRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();
        try { r.Schedule(request.ScheduledAtUtc); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        await _db.SaveChangesAsync(ct);
        return Ok(r);
    }

    public record CompleteRequest(string TestResult, string AccuracyResult, string? FinalRemarks);

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteRequest request, CancellationToken ct)
    {
        var r = await _db.MeterTestingRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();
        try { r.Complete(request.TestResult, request.AccuracyResult, request.FinalRemarks); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { return Conflict(ex.Message); }
        await _db.SaveChangesAsync(ct);
        return Ok(r);
    }
}
