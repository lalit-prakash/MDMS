using MDMS.Application.Common;
using MDMS.Application.EnergyAudit;
using MDMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Energy-audit: comparing energy entering a network level against energy accounted for
/// downstream. This project has no feeder/DTR meter ingestion pipeline yet, so a network energy
/// reading is entered directly here rather than derived from a real boundary meter feed.
/// </summary>
[ApiController]
[Route("api/v1/energy-audit")]
public class EnergyAuditController : ControllerBase
{
    private readonly IMdmsDbContext _db;
    private readonly EnergyAuditService _service;

    public EnergyAuditController(IMdmsDbContext db, EnergyAuditService service)
    {
        _db = db;
        _service = service;
    }

    public record RecordNetworkEnergyRequest(Guid HierarchyNodeId, DateOnly Date, decimal EnergyKwh);

    /// <summary>Records (or revises) the supply-side energy for one hierarchy node/day.</summary>
    [HttpPost("network-energy-readings")]
    public async Task<IActionResult> RecordNetworkEnergy([FromBody] RecordNetworkEnergyRequest request, CancellationToken ct)
    {
        var nodeExists = await _db.HierarchyNodes.AnyAsync(n => n.Id == request.HierarchyNodeId, ct);
        if (!nodeExists)
            return NotFound($"Hierarchy node {request.HierarchyNodeId} not found.");

        var existing = await _db.NetworkEnergyReadings.FirstOrDefaultAsync(
            r => r.HierarchyNodeId == request.HierarchyNodeId && r.Date == request.Date, ct);

        NetworkEnergyReading reading;
        try
        {
            if (existing is not null)
            {
                existing.Revise(request.EnergyKwh);
                reading = existing;
            }
            else
            {
                reading = new NetworkEnergyReading(request.HierarchyNodeId, request.Date, request.EnergyKwh);
                _db.NetworkEnergyReadings.Add(reading);
            }
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(reading);
    }

    [HttpGet("network-energy-readings")]
    public async Task<IActionResult> ListNetworkEnergyReadings([FromQuery] Guid? hierarchyNodeId, CancellationToken ct)
    {
        var query = _db.NetworkEnergyReadings.AsQueryable();
        if (hierarchyNodeId.HasValue)
            query = query.Where(r => r.HierarchyNodeId == hierarchyNodeId.Value);

        var readings = await query.OrderByDescending(r => r.Date).Take(500).ToListAsync(ct);
        return Ok(readings);
    }

    /// <summary>
    /// The transparent energy balance for a node/day: supply-side reading (if any), summed
    /// downstream consumption, the discrepancy, and data completeness — never a single collapsed
    /// loss percentage (see <see cref="EnergyBalanceResult"/>).
    /// </summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance([FromQuery] Guid hierarchyNodeId, [FromQuery] DateOnly date, CancellationToken ct)
    {
        var result = await _service.ComputeDailyBalanceAsync(hierarchyNodeId, date, ct);
        return Ok(result);
    }
}
