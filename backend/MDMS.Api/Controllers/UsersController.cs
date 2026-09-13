using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// User identity/role/org-scope records, plus (as of the auth work) whether each has claimed a
/// password yet. Every response here is a <see cref="UserResponse"/> projection, never the raw
/// <see cref="User"/> entity — the entity carries <c>PasswordHash</c>, and a hash is still a
/// secret that must never leave the server, even hashed.
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public UsersController(IMdmsDbContext db) => _db = db;

    public record CreateUserRequest(string Username, string DisplayName, UserRole Role, Guid? OrgUnitId);
    public record UserResponse(Guid Id, string Username, string DisplayName, UserRole Role, Guid? OrgUnitId, bool HasPassword, DateTime CreatedAtUtc);

    private static UserResponse ToResponse(User u) =>
        new(u.Id, u.Username, u.DisplayName, u.Role, u.OrgUnitId, u.PasswordHash is not null, u.CreatedAtUtc);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] UserRole? role, CancellationToken ct)
    {
        var query = _db.Users.AsQueryable();
        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        var users = await query.OrderBy(u => u.Username).ToListAsync(ct);
        return Ok(users.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? NotFound() : Ok(ToResponse(user));
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (request.OrgUnitId is Guid orgUnitId)
        {
            var orgUnitExists = await _db.OrgUnits.AnyAsync(u => u.Id == orgUnitId, ct);
            if (!orgUnitExists)
                return NotFound($"Org unit {orgUnitId} not found.");
        }

        User user;
        try
        {
            user = new User(request.Username, request.DisplayName, request.Role, request.OrgUnitId);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, ToResponse(user));
    }

    public record ReassignUserRequest(UserRole Role, Guid? OrgUnitId);

    [HttpPost("{id:guid}/reassign")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Reassign(Guid id, [FromBody] ReassignUserRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();

        if (request.OrgUnitId is Guid orgUnitId)
        {
            var orgUnitExists = await _db.OrgUnits.AnyAsync(u => u.Id == orgUnitId, ct);
            if (!orgUnitExists)
                return NotFound($"Org unit {orgUnitId} not found.");
        }

        try
        {
            user.Reassign(request.Role, request.OrgUnitId);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(user));
    }
}
