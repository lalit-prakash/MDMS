using MDMS.Application.Common;
using MDMS.Application.RevenueProtection;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Revenue-protection leads: Detected → Scored → Reviewed → Assigned → FieldInvestigation →
/// FindingRecorded → ActionTaken → (optionally) RecoveryRecorded → Closed. An investigation
/// lead-generation pipeline, never a legal conclusion — see <see cref="RevenueProtectionLead"/>.
/// </summary>
[ApiController]
[Route("api/v1/revenue-protection")]
public class RevenueProtectionController : ControllerBase
{
    private readonly IMdmsDbContext _db;
    private readonly RevenueProtectionService _service;

    public RevenueProtectionController(IMdmsDbContext db, RevenueProtectionService service)
    {
        _db = db;
        _service = service;
    }

    public record RaiseSignalRequest(Guid CustomerId, Guid? MeterId, RiskSignalType SignalType, decimal Weight, string? Note);

    /// <summary>
    /// Raises a risk signal, creating a lead if the customer has none currently open. The weight
    /// is supplied by the caller — this project has no built-in opinion on what a signal type is
    /// worth; that calibration is the utility's job.
    /// </summary>
    [HttpPost("signals")]
    public async Task<IActionResult> RaiseSignal([FromBody] RaiseSignalRequest request, CancellationToken ct)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists) return NotFound($"Customer {request.CustomerId} not found.");

        try
        {
            var signal = await _service.RaiseSignalAsync(
                request.CustomerId, request.MeterId, request.SignalType, request.Weight, request.Note, ct);
            return Ok(signal);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("leads")]
    public async Task<IActionResult> ListLeads([FromQuery] RevenueProtectionLeadStatus? status, CancellationToken ct)
    {
        var query = _db.RevenueProtectionLeads.AsQueryable();
        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        return Ok(await query.OrderByDescending(l => l.RiskScore).Take(500).ToListAsync(ct));
    }

    [HttpGet("leads/{id:guid}")]
    public async Task<IActionResult> GetLead(Guid id, CancellationToken ct)
    {
        var lead = await _db.RevenueProtectionLeads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null) return NotFound();

        var signals = await _db.RiskSignals.Where(s => s.LeadId == id).OrderBy(s => s.CreatedAtUtc).ToListAsync(ct);
        return Ok(new { lead, signals });
    }

    public record UserDecisionRequest(Guid UserId);
    public record NoteRequest(string Note);
    public record RecoveryRequest(decimal Amount);

    [HttpPost("leads/{id:guid}/review")]
    public Task<IActionResult> Review(Guid id, CancellationToken ct) => Apply(id, ct, l => l.Review());

    [HttpPost("leads/{id:guid}/assign")]
    public Task<IActionResult> Assign(Guid id, [FromBody] UserDecisionRequest request, CancellationToken ct)
        => Apply(id, ct, l => l.AssignTo(request.UserId));

    [HttpPost("leads/{id:guid}/start-investigation")]
    public Task<IActionResult> StartInvestigation(Guid id, CancellationToken ct) => Apply(id, ct, l => l.StartFieldInvestigation());

    [HttpPost("leads/{id:guid}/record-finding")]
    public Task<IActionResult> RecordFinding(Guid id, [FromBody] NoteRequest request, CancellationToken ct)
        => Apply(id, ct, l => l.RecordFinding(request.Note));

    [HttpPost("leads/{id:guid}/record-action")]
    public Task<IActionResult> RecordAction(Guid id, [FromBody] NoteRequest request, CancellationToken ct)
        => Apply(id, ct, l => l.RecordAction(request.Note));

    [HttpPost("leads/{id:guid}/record-recovery")]
    public Task<IActionResult> RecordRecovery(Guid id, [FromBody] RecoveryRequest request, CancellationToken ct)
        => Apply(id, ct, l => l.RecordRecovery(request.Amount));

    [HttpPost("leads/{id:guid}/close")]
    public Task<IActionResult> Close(Guid id, [FromBody] NoteRequest request, CancellationToken ct)
        => Apply(id, ct, l => l.Close(request.Note));

    private async Task<IActionResult> Apply(Guid id, CancellationToken ct, Action<RevenueProtectionLead> transition)
    {
        var lead = await _db.RevenueProtectionLeads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null) return NotFound();

        try
        {
            transition(lead);
        }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

        await _db.SaveChangesAsync(ct);
        return Ok(lead);
    }
}
