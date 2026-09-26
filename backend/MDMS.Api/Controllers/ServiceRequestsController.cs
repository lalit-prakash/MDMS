using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>Consumer service requests (meter replacement/shifting, load change, name/contact/
/// address correction, prepaid-postpaid conversion) — separate from Complaints per the reference
/// spec's own module split.</summary>
[ApiController]
[Route("api/v1/service-requests")]
public class ServiceRequestsController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public ServiceRequestsController(IMdmsDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? customerId, CancellationToken ct)
    {
        var query = _db.ServiceRequests.AsQueryable();
        if (customerId.HasValue) query = query.Where(r => r.CustomerId == customerId.Value);
        var rows = await query.OrderByDescending(r => r.CreatedAtUtc).Take(500).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        return r is null ? NotFound() : Ok(r);
    }

    public record CreateServiceRequest(Guid CustomerId, ServiceRequestCategory Category, string Description);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request, CancellationToken ct)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists) return NotFound($"Customer {request.CustomerId} not found.");

        ServiceRequest entity;
        try { entity = new ServiceRequest(request.CustomerId, request.Category, request.Description); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

        _db.ServiceRequests.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var r = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();
        try { r.Cancel(); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        await _db.SaveChangesAsync(ct);
        return Ok(r);
    }
}
