using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Master/reference-data configuration: tariff categories, the electrical hierarchy
/// (Substation/Feeder/DT), and the organizational hierarchy (Zone/Circle/Division/Sub
/// Division/Section) — two separate trees for two separate purposes, see
/// <see cref="HierarchyNodeType"/> and <see cref="OrgUnitType"/>. Meter and Customer master data
/// have their own controllers (<c>MetersController</c>); VEE rule configuration lives under
/// <c>/api/v1/vee</c> (<c>VeeController</c>).
/// </summary>
/// <remarks>
/// No role/authorization check is applied yet — that needs an auth package decision (JWT bearer
/// vs. another scheme) before it can be added; see the project notes for that open question.
/// </remarks>
[ApiController]
[Route("api/v1/config")]
public class ConfigController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public ConfigController(IMdmsDbContext db) => _db = db;

    // ----- Tariff categories -----

    public record CreateTariffCategoryRequest(string Code, string Name, string? Description);

    [HttpGet("tariff-categories")]
    public async Task<IActionResult> ListTariffCategories(CancellationToken ct)
    {
        var categories = await _db.TariffCategories.OrderBy(c => c.Code).ToListAsync(ct);
        return Ok(categories);
    }

    [HttpPost("tariff-categories")]
    public async Task<IActionResult> CreateTariffCategory([FromBody] CreateTariffCategoryRequest request, CancellationToken ct)
    {
        TariffCategory category;
        try
        {
            category = new TariffCategory(request.Code, request.Name, request.Description);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        _db.TariffCategories.Add(category);
        await _db.SaveChangesAsync(ct);

        return Ok(category);
    }

    // ----- Electrical hierarchy (Substation / Feeder / Distribution Transformer) -----

    public record CreateHierarchyNodeRequest(HierarchyNodeType NodeType, Guid? ParentId, string Code, string Name);

    [HttpGet("hierarchy")]
    public async Task<IActionResult> ListHierarchyNodes([FromQuery] HierarchyNodeType? nodeType, CancellationToken ct)
    {
        var query = _db.HierarchyNodes.AsQueryable();
        if (nodeType.HasValue)
            query = query.Where(n => n.NodeType == nodeType.Value);

        var nodes = await query.OrderBy(n => n.NodeType).ThenBy(n => n.Code).ToListAsync(ct);
        return Ok(nodes);
    }

    [HttpGet("hierarchy/{id:guid}/children")]
    public async Task<IActionResult> ListHierarchyChildren(Guid id, CancellationToken ct)
    {
        var children = await _db.HierarchyNodes
            .Where(n => n.ParentId == id)
            .OrderBy(n => n.Code)
            .ToListAsync(ct);

        return Ok(children);
    }

    [HttpPost("hierarchy")]
    public async Task<IActionResult> CreateHierarchyNode([FromBody] CreateHierarchyNodeRequest request, CancellationToken ct)
    {
        HierarchyNode node;
        try
        {
            if (request.NodeType == HierarchyNodeType.Substation)
            {
                node = HierarchyNode.CreateSubstation(request.Code, request.Name);
            }
            else
            {
                if (request.ParentId is not Guid parentId)
                    return BadRequest($"A {request.NodeType} requires a ParentId.");

                var parent = await _db.HierarchyNodes.FirstOrDefaultAsync(n => n.Id == parentId, ct);
                if (parent is null)
                    return NotFound($"Parent node {parentId} not found.");

                node = HierarchyNode.CreateChild(request.NodeType, parent, request.Code, request.Name);
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        _db.HierarchyNodes.Add(node);
        await _db.SaveChangesAsync(ct);

        return Ok(node);
    }

    public record AssignOrgUnitRequest(Guid OrgUnitId);

    /// <summary>Links a Substation to its administrative office — the join that lets a Region/
    /// Zone/Circle/Division/Sub-Division filter resolve down to Feeders/DTRs/Consumers via
    /// <see cref="NetworkController"/>.</summary>
    [HttpPost("hierarchy/{id:guid}/org-unit")]
    public async Task<IActionResult> AssignOrgUnit(Guid id, [FromBody] AssignOrgUnitRequest request, CancellationToken ct)
    {
        var node = await _db.HierarchyNodes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (node is null) return NotFound();

        var orgUnit = await _db.OrgUnits.FirstOrDefaultAsync(u => u.Id == request.OrgUnitId, ct);
        if (orgUnit is null) return NotFound($"Org unit {request.OrgUnitId} not found.");

        try
        {
            node.AssignOrgUnit(orgUnit);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(node);
    }

    public record SetHierarchyMasterDataRequest(
        decimal? CapacityKva, string? VoltageLevel, string? Make,
        DateOnly? CommissionedOn, string? OperationalStatus, decimal? Latitude, decimal? Longitude);

    /// <summary>Sets the optional Feeder/DTR master-data fields (capacity, make, commissioning
    /// date, etc.) per the reference Feeder/DTR master-info sheets.</summary>
    [HttpPost("hierarchy/{id:guid}/master-data")]
    public async Task<IActionResult> SetHierarchyMasterData(Guid id, [FromBody] SetHierarchyMasterDataRequest request, CancellationToken ct)
    {
        var node = await _db.HierarchyNodes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (node is null) return NotFound();

        node.SetMasterData(request.CapacityKva, request.VoltageLevel, request.Make, request.CommissionedOn, request.OperationalStatus, request.Latitude, request.Longitude);
        await _db.SaveChangesAsync(ct);
        return Ok(node);
    }

    // ----- Organizational hierarchy (Zone / Circle / Division / Sub Division / Section) -----

    public record CreateOrgUnitRequest(OrgUnitType UnitType, Guid? ParentId, string Code, string Name);

    [HttpGet("org-units")]
    public async Task<IActionResult> ListOrgUnits([FromQuery] OrgUnitType? unitType, CancellationToken ct)
    {
        var query = _db.OrgUnits.AsQueryable();
        if (unitType.HasValue)
            query = query.Where(u => u.UnitType == unitType.Value);

        var units = await query.OrderBy(u => u.UnitType).ThenBy(u => u.Code).ToListAsync(ct);
        return Ok(units);
    }

    [HttpGet("org-units/{id:guid}/children")]
    public async Task<IActionResult> ListOrgUnitChildren(Guid id, CancellationToken ct)
    {
        var children = await _db.OrgUnits
            .Where(u => u.ParentId == id)
            .OrderBy(u => u.Code)
            .ToListAsync(ct);

        return Ok(children);
    }

    [HttpPost("org-units")]
    public async Task<IActionResult> CreateOrgUnit([FromBody] CreateOrgUnitRequest request, CancellationToken ct)
    {
        OrgUnit unit;
        try
        {
            if (request.UnitType == OrgUnitType.Zone)
            {
                unit = OrgUnit.CreateZone(request.Code, request.Name);
            }
            else
            {
                if (request.ParentId is not Guid parentId)
                    return BadRequest($"A {request.UnitType} requires a ParentId.");

                var parent = await _db.OrgUnits.FirstOrDefaultAsync(u => u.Id == parentId, ct);
                if (parent is null)
                    return NotFound($"Parent org unit {parentId} not found.");

                unit = OrgUnit.CreateChild(request.UnitType, parent, request.Code, request.Name);
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        _db.OrgUnits.Add(unit);
        await _db.SaveChangesAsync(ct);

        return Ok(unit);
    }
}
