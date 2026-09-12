using MDMS.Application.Common;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// User identity/role/org-scope records. This is data only — there is no login, credential, or
/// permission-enforcement mechanism yet (a real `iam` module needs an auth-package decision
/// first). Storing the model now lets other modules (once built) reference a real user rather
/// than a free-text name.
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public UsersController(IMdmsDbContext db) => _db = db;

    public record CreateUserRequest(string Username, string DisplayName, UserRole Role, Guid? OrgUnitId);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] UserRole? role, CancellationToken ct)
    {
        var query = _db.Users.AsQueryable();
        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        var users = await query.OrderBy(u => u.Username).ToListAsync(ct);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
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

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    public record ReassignUserRequest(UserRole Role, Guid? OrgUnitId);

    [HttpPost("{id:guid}/reassign")]
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
        return Ok(user);
    }
}
