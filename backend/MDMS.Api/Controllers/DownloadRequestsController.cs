using MDMS.Api.Reporting;
using MDMS.Application.Common;
using MDMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>Asynchronous export requests, always scoped to the signed-in user.</summary>
[ApiController]
[Route("api/v1/download-requests")]
public class DownloadRequestsController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public DownloadRequestsController(IMdmsDbContext db) => _db = db;

    private Guid? CurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    public record DownloadRequestResponse(
        Guid Id, string Title, string Status, DateTime RequestedAtUtc, DateTime? CompletedAtUtc,
        string? FileName, long? SizeBytes, int? RowCount, string? ErrorMessage);

    private static DownloadRequestResponse ToResponse(DownloadRequest r) => new(
        r.Id, r.Title, r.Status.ToString(), r.CreatedAtUtc, r.CompletedAtUtc, r.FileName, r.SizeBytes, r.RowCount, r.ErrorMessage);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (CurrentUserId() is not Guid userId) return Unauthorized();
        // Content is deliberately not projected, so listing never pulls file bodies out of the table.
        var rows = await _db.DownloadRequests.Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc).Take(50)
            .Select(r => new DownloadRequestResponse(r.Id, r.Title, r.Status.ToString(), r.CreatedAtUtc, r.CompletedAtUtc, r.FileName, r.SizeBytes, r.RowCount, r.ErrorMessage))
            .ToListAsync(ct);
        return Ok(rows);
    }

    public record CreateDownloadRequest(string Title, string Path);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDownloadRequest request, CancellationToken ct)
    {
        if (CurrentUserId() is not Guid userId) return Unauthorized();
        var path = DownloadRequestPaths.Normalize(request.Path ?? "");
        if (!DownloadRequestPaths.IsAllowed(path))
            return BadRequest("That screen cannot be exported as a download request.");

        DownloadRequest entity;
        try { entity = new DownloadRequest(userId, request.Title, path); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

        _db.DownloadRequests.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(entity));
    }

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken ct)
    {
        if (CurrentUserId() is not Guid userId) return Unauthorized();
        var r = await _db.DownloadRequests.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (r is null) return NotFound();
        if (r.Content is null) return Conflict("This download is not ready.");
        return File(r.Content, "text/csv", r.FileName ?? "MDMS_Export.csv");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (CurrentUserId() is not Guid userId) return Unauthorized();
        var r = await _db.DownloadRequests.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (r is null) return NotFound();
        _db.DownloadRequests.Remove(r);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
