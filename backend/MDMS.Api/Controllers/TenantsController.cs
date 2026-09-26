using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Organisations (tenants): which ones the caller can switch between, and — for Admins —
/// creating an organisation and granting a user access to it. All operational data is isolated
/// per organisation (see MdmsDbContext's tenant filter); this controller only manages the registry.
/// </summary>
[ApiController]
[Route("api/v1/tenants")]
public class TenantsController : ControllerBase
{
    private readonly IMdmsDbContext _db;
    private readonly ITenantContext _tenant;

    public TenantsController(IMdmsDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private Guid? CurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private Guid? HomeTenantId() => Guid.TryParse(User.FindFirst("tenant")?.Value, out var id) ? id : null;

    public record TenantResponse(Guid Id, string Code, string Name, bool IsCurrent, bool IsHome);

    private async Task<List<Guid>> AccessibleTenantIdsAsync(Guid userId, Guid home, CancellationToken ct)
    {
        var granted = await _db.UserTenantAccesses.Where(a => a.UserId == userId).Select(a => a.GrantedTenantId).ToListAsync(ct);
        granted.Add(home);
        return granted.Distinct().ToList();
    }

    /// <summary>The organisations the signed-in user can switch to, marking the active one.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (CurrentUserId() is not Guid userId || HomeTenantId() is not Guid home) return Unauthorized();
        var ids = await AccessibleTenantIdsAsync(userId, home, ct);
        var tenants = await _db.Tenants.Where(t => ids.Contains(t.Id)).OrderBy(t => t.Name).ToListAsync(ct);
        return Ok(tenants.Select(t => new TenantResponse(t.Id, t.Code, t.Name, t.Id == _tenant.TenantId, t.Id == home)));
    }

    public record CreateTenantRequest(string Code, string Name);

    /// <summary>Creates an organisation and grants the creating Admin access to it.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        if (CurrentUserId() is not Guid userId || HomeTenantId() is not Guid home) return Unauthorized();

        Tenant tenant;
        try { tenant = new Tenant(request.Code.Trim(), request.Name.Trim()); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

        if (await _db.Tenants.AnyAsync(t => t.Code == tenant.Code, ct))
            return Conflict($"An organisation with code '{tenant.Code}' already exists.");

        _db.Tenants.Add(tenant);
        if (tenant.Id != home)
            _db.UserTenantAccesses.Add(new UserTenantAccess(userId, tenant.Id));
        await _db.SaveChangesAsync(ct);
        return Ok(new TenantResponse(tenant.Id, tenant.Code, tenant.Name, false, false));
    }

    public record GrantAccessRequest(string Username);

    /// <summary>Lets a user switch into an organisation. The granting Admin must themselves have access to it.</summary>
    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> GrantAccess(Guid id, [FromBody] GrantAccessRequest request, CancellationToken ct)
    {
        if (CurrentUserId() is not Guid adminId || HomeTenantId() is not Guid home) return Unauthorized();
        if (!(await AccessibleTenantIdsAsync(adminId, home, ct)).Contains(id))
            return Forbid();

        var target = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Username == request.Username.Trim(), ct);
        if (target is null) return NotFound($"User '{request.Username}' not found.");
        if (target.TenantId == id || await _db.UserTenantAccesses.AnyAsync(a => a.UserId == target.Id && a.GrantedTenantId == id, ct))
            return Ok(new { message = "User already has access." });

        _db.UserTenantAccesses.Add(new UserTenantAccess(target.Id, id));
        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Access granted." });
    }
}
