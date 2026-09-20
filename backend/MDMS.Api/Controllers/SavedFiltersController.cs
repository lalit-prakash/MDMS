using System.Text.Json;
using MDMS.Application.Common;
using MDMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Per-user saved filter sets for any list screen. Always scoped to the signed-in user (from the
/// JWT) — one user can never list, load or delete another's saved filters.
/// </summary>
[ApiController]
[Route("api/v1/saved-filters")]
public class SavedFiltersController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public SavedFiltersController(IMdmsDbContext db) => _db = db;

    private Guid? CurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    public record SavedFilterResponse(Guid Id, string Screen, string Name, Dictionary<string, string> Values, DateTime CreatedAtUtc);

    private static SavedFilterResponse ToResponse(SavedFilter f) => new(
        f.Id, f.Screen, f.Name,
        JsonSerializer.Deserialize<Dictionary<string, string>>(f.FilterJson) ?? new(),
        f.CreatedAtUtc);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string screen, CancellationToken ct)
    {
        if (CurrentUserId() is not Guid userId) return Unauthorized();
        var filters = await _db.SavedFilters.Where(f => f.UserId == userId && f.Screen == screen).OrderBy(f => f.Name).ToListAsync(ct);
        return Ok(filters.Select(ToResponse));
    }

    public record SaveFilterRequest(string Screen, string Name, Dictionary<string, string> Values);

    /// <summary>Saves a filter set; saving under an existing name for the same screen replaces it.</summary>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveFilterRequest request, CancellationToken ct)
    {
        if (CurrentUserId() is not Guid userId) return Unauthorized();
        if (request.Values is null || request.Values.Count == 0)
            return BadRequest("There are no applied filters to save.");

        var json = JsonSerializer.Serialize(request.Values);
        try
        {
            var filter = new SavedFilter(userId, request.Screen, request.Name, json);
            var existing = await _db.SavedFilters.FirstOrDefaultAsync(
                f => f.UserId == userId && f.Screen == filter.Screen && f.Name == filter.Name, ct);
            if (existing is not null)
            {
                // Committed first so the unique (user, screen, name) index never sees both rows.
                _db.SavedFilters.Remove(existing);
                await _db.SaveChangesAsync(ct);
            }
            _db.SavedFilters.Add(filter);
            await _db.SaveChangesAsync(ct);
            return Ok(ToResponse(filter));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (CurrentUserId() is not Guid userId) return Unauthorized();
        var filter = await _db.SavedFilters.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, ct);
        if (filter is null) return NotFound();
        _db.SavedFilters.Remove(filter);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
