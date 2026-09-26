using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Consumer complaint tracking: Open → Assigned → InProgress → Resolved → Closed, with an SLA
/// due-time computed from a caller-supplied duration (no hard-coded SLA hours — a future SLA
/// policy configuration would supply this instead).
/// </summary>
[ApiController]
[Route("api/v1/complaints")]
public class ComplaintsController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public ComplaintsController(IMdmsDbContext db) => _db = db;

    public record CreateComplaintRequest(
        Guid CustomerId, Guid? MeterId, ComplaintSource Source, string Description, double SlaHours);

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ComplaintStatus? status, [FromQuery] bool overdueOnly, [FromQuery] Guid? customerId, CancellationToken ct)
    {
        var query = _db.Complaints.AsQueryable();
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);
        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);

        var complaints = await query.OrderByDescending(c => c.CreatedAtUtc).Take(500).ToListAsync(ct);
        if (overdueOnly)
        {
            var now = DateTime.UtcNow;
            complaints = complaints.Where(c => c.IsOverdue(now)).ToList();
        }

        return Ok(complaints);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.Id == id, ct);
        return complaint is null ? NotFound() : Ok(complaint);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateComplaintRequest request, CancellationToken ct)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists)
            return NotFound($"Customer {request.CustomerId} not found.");

        Complaint complaint;
        try
        {
            complaint = new Complaint(
                request.CustomerId, request.MeterId, request.Source, request.Description,
                TimeSpan.FromHours(request.SlaHours));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        _db.Complaints.Add(complaint);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = complaint.Id }, complaint);
    }

    public record AssignComplaintRequest(Guid UserId);

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignComplaintRequest request, CancellationToken ct)
        => await ApplyTransition(id, ct, c => c.AssignTo(request.UserId));

    [HttpPost("{id:guid}/start-progress")]
    public async Task<IActionResult> StartProgress(Guid id, CancellationToken ct)
        => await ApplyTransition(id, ct, c => c.StartProgress());

    public record ResolveComplaintRequest(string ResolutionNote);

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveComplaintRequest request, CancellationToken ct)
        => await ApplyTransition(id, ct, c => c.Resolve(request.ResolutionNote));

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
        => await ApplyTransition(id, ct, c => c.Close());

    private async Task<IActionResult> ApplyTransition(Guid id, CancellationToken ct, Action<Complaint> transition)
    {
        var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (complaint is null) return NotFound();

        try
        {
            transition(complaint);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(complaint);
    }
}
