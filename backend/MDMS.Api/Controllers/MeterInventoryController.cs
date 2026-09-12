using MDMS.Application.Common;
using MDMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Meter inventory allocation (Received → InStore → AssignedToContractor → AssignedToInstaller →
/// Installed → Commissioned → Active, plus the replacement sub-flow) and the three-level
/// installation quality check (Contractor L1 → Quality Incharge L2 → Utility Manager L3 → RMS
/// sync → MIS onboarding) — two separate state machines per meter, matching the spec's own
/// separation of "is this meter allocated/installed" from "was that installation quality-approved".
/// </summary>
[ApiController]
[Route("api/v1/meter-inventory")]
public class MeterInventoryController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public MeterInventoryController(IMdmsDbContext db) => _db = db;

    // ----- Inventory allocation -----

    [HttpPost("{meterId:guid}/receive")]
    public async Task<IActionResult> Receive(Guid meterId, CancellationToken ct)
    {
        var meterExists = await _db.Meters.AnyAsync(m => m.Id == meterId, ct);
        if (!meterExists) return NotFound($"Meter {meterId} not found.");

        var alreadyExists = await _db.MeterInventoryRecords.AnyAsync(r => r.MeterId == meterId, ct);
        if (alreadyExists) return Conflict("An inventory record already exists for this meter.");

        var record = MeterInventoryRecord.CreateReceived(meterId);
        _db.MeterInventoryRecords.Add(record);
        await _db.SaveChangesAsync(ct);
        return Ok(record);
    }

    [HttpGet("{meterId:guid}")]
    public async Task<IActionResult> GetInventory(Guid meterId, CancellationToken ct)
    {
        var record = await _db.MeterInventoryRecords.FirstOrDefaultAsync(r => r.MeterId == meterId, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpGet]
    public async Task<IActionResult> ListInventory([FromQuery] Domain.Enums.MeterInventoryStatus? status, CancellationToken ct)
    {
        var query = _db.MeterInventoryRecords.AsQueryable();
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return Ok(await query.OrderByDescending(r => r.CreatedAtUtc).Take(500).ToListAsync(ct));
    }

    public record AssignContractorRequest(Guid ContractorUserId);
    public record AssignInstallerRequest(Guid InstallerUserId);
    public record RequestReplacementRequest(string Reason);

    [HttpPost("{meterId:guid}/move-to-store")]
    public Task<IActionResult> MoveToStore(Guid meterId, CancellationToken ct) => ApplyInventory(meterId, ct, r => r.MoveToStore());

    [HttpPost("{meterId:guid}/assign-contractor")]
    public Task<IActionResult> AssignContractor(Guid meterId, [FromBody] AssignContractorRequest request, CancellationToken ct)
        => ApplyInventory(meterId, ct, r => r.AssignToContractor(request.ContractorUserId));

    [HttpPost("{meterId:guid}/assign-installer")]
    public Task<IActionResult> AssignInstaller(Guid meterId, [FromBody] AssignInstallerRequest request, CancellationToken ct)
        => ApplyInventory(meterId, ct, r => r.AssignToInstaller(request.InstallerUserId));

    [HttpPost("{meterId:guid}/mark-installed")]
    public Task<IActionResult> MarkInstalled(Guid meterId, CancellationToken ct) => ApplyInventory(meterId, ct, r => r.MarkInstalled());

    [HttpPost("{meterId:guid}/commission")]
    public Task<IActionResult> Commission(Guid meterId, CancellationToken ct) => ApplyInventory(meterId, ct, r => r.Commission());

    [HttpPost("{meterId:guid}/activate")]
    public Task<IActionResult> Activate(Guid meterId, CancellationToken ct) => ApplyInventory(meterId, ct, r => r.Activate());

    [HttpPost("{meterId:guid}/request-replacement")]
    public Task<IActionResult> RequestReplacement(Guid meterId, [FromBody] RequestReplacementRequest request, CancellationToken ct)
        => ApplyInventory(meterId, ct, r => r.RequestReplacement(request.Reason));

    [HttpPost("{meterId:guid}/mark-removed")]
    public Task<IActionResult> MarkRemoved(Guid meterId, CancellationToken ct) => ApplyInventory(meterId, ct, r => r.MarkRemoved());

    private async Task<IActionResult> ApplyInventory(Guid meterId, CancellationToken ct, Action<MeterInventoryRecord> transition)
    {
        var record = await _db.MeterInventoryRecords.FirstOrDefaultAsync(r => r.MeterId == meterId, ct);
        if (record is null) return NotFound("No inventory record for this meter — call /receive first.");

        try
        {
            transition(record);
        }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

        await _db.SaveChangesAsync(ct);
        return Ok(record);
    }

    // ----- Installation quality check -----

    [HttpPost("{meterId:guid}/quality-check")]
    public async Task<IActionResult> CreateQualityCheck(Guid meterId, CancellationToken ct)
    {
        var meterExists = await _db.Meters.AnyAsync(m => m.Id == meterId, ct);
        if (!meterExists) return NotFound($"Meter {meterId} not found.");

        var check = InstallationQualityCheck.Create(meterId);
        _db.InstallationQualityChecks.Add(check);
        await _db.SaveChangesAsync(ct);
        return Ok(check);
    }

    [HttpGet("quality-checks/{id:guid}")]
    public async Task<IActionResult> GetQualityCheck(Guid id, CancellationToken ct)
    {
        var check = await _db.InstallationQualityChecks.FirstOrDefaultAsync(c => c.Id == id, ct);
        return check is null ? NotFound() : Ok(check);
    }

    [HttpGet("quality-checks")]
    public async Task<IActionResult> ListQualityChecks([FromQuery] Domain.Enums.InstallationQualityStatus? status, CancellationToken ct)
    {
        var query = _db.InstallationQualityChecks.AsQueryable();
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return Ok(await query.OrderByDescending(c => c.CreatedAtUtc).Take(500).ToListAsync(ct));
    }

    public record UserDecisionRequest(Guid UserId);
    public record RejectionRequest(Guid UserId, string Note);

    [HttpPost("quality-checks/{id:guid}/start-l1")]
    public Task<IActionResult> StartL1(Guid id, CancellationToken ct) => ApplyQc(id, ct, c => c.StartL1());

    [HttpPost("quality-checks/{id:guid}/complete-l1")]
    public Task<IActionResult> CompleteL1(Guid id, [FromBody] UserDecisionRequest request, CancellationToken ct)
        => ApplyQc(id, ct, c => c.CompleteL1(request.UserId));

    [HttpPost("quality-checks/{id:guid}/submit-l2")]
    public Task<IActionResult> SubmitToL2(Guid id, CancellationToken ct) => ApplyQc(id, ct, c => c.SubmitToL2());

    [HttpPost("quality-checks/{id:guid}/approve-l2")]
    public Task<IActionResult> ApproveL2(Guid id, [FromBody] UserDecisionRequest request, CancellationToken ct)
        => ApplyQc(id, ct, c => c.ApproveL2(request.UserId));

    [HttpPost("quality-checks/{id:guid}/reject-l2")]
    public Task<IActionResult> RejectL2(Guid id, [FromBody] RejectionRequest request, CancellationToken ct)
        => ApplyQc(id, ct, c => c.RejectL2(request.UserId, request.Note));

    [HttpPost("quality-checks/{id:guid}/submit-l3")]
    public Task<IActionResult> SubmitToL3(Guid id, CancellationToken ct) => ApplyQc(id, ct, c => c.SubmitToL3());

    [HttpPost("quality-checks/{id:guid}/approve-l3")]
    public Task<IActionResult> ApproveL3(Guid id, [FromBody] UserDecisionRequest request, CancellationToken ct)
        => ApplyQc(id, ct, c => c.ApproveL3(request.UserId));

    [HttpPost("quality-checks/{id:guid}/reject-l3")]
    public Task<IActionResult> RejectL3(Guid id, [FromBody] RejectionRequest request, CancellationToken ct)
        => ApplyQc(id, ct, c => c.RejectL3(request.UserId, request.Note));

    [HttpPost("quality-checks/{id:guid}/rms-sync-pending")]
    public Task<IActionResult> MarkRmsSyncPending(Guid id, CancellationToken ct) => ApplyQc(id, ct, c => c.MarkRmsSyncPending());

    [HttpPost("quality-checks/{id:guid}/rms-synced")]
    public Task<IActionResult> MarkRmsSynced(Guid id, CancellationToken ct) => ApplyQc(id, ct, c => c.MarkRmsSynced());

    [HttpPost("quality-checks/{id:guid}/mis-onboarded")]
    public Task<IActionResult> MarkMisOnboarded(Guid id, CancellationToken ct) => ApplyQc(id, ct, c => c.MarkMisOnboarded());

    private async Task<IActionResult> ApplyQc(Guid id, CancellationToken ct, Action<InstallationQualityCheck> transition)
    {
        var check = await _db.InstallationQualityChecks.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (check is null) return NotFound();

        try
        {
            transition(check);
        }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

        await _db.SaveChangesAsync(ct);
        return Ok(check);
    }
}
