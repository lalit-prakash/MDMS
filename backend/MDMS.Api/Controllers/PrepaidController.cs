using MDMS.Application.Common;
using MDMS.Application.Prepaid;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Prepaid wallet: recharge, daily billing from validated Daily Load Profile consumption, and
/// manual connect/disconnect. No HES/meter command integration exists here — "reconnect" only
/// flips this account's own status, it never dispatches or confirms a physical meter action.
/// </summary>
[ApiController]
[Route("api/v1/prepaid")]
public class PrepaidController : ControllerBase
{
    private readonly IMdmsDbContext _db;
    private readonly PrepaidService _service;

    public PrepaidController(IMdmsDbContext db, PrepaidService service)
    {
        _db = db;
        _service = service;
    }

    [HttpGet("accounts/{customerId:guid}")]
    public async Task<IActionResult> GetAccount(Guid customerId, CancellationToken ct)
    {
        var account = await _db.PrepaidAccounts.FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpGet("accounts/{customerId:guid}/transactions")]
    public async Task<IActionResult> ListTransactions(Guid customerId, CancellationToken ct)
    {
        var account = await _db.PrepaidAccounts.FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);
        if (account is null) return NotFound();

        var transactions = await _db.WalletTransactions
            .Where(t => t.PrepaidAccountId == account.Id)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(500)
            .ToListAsync(ct);

        return Ok(transactions);
    }

    public record RechargeRequest(Guid CustomerId, decimal Amount, string Reference);

    /// <summary>Idempotent: a repeated Reference (e.g. a duplicated payment-gateway callback) never double-credits the wallet.</summary>
    [HttpPost("recharge")]
    public async Task<IActionResult> Recharge([FromBody] RechargeRequest request, CancellationToken ct)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists) return NotFound($"Customer {request.CustomerId} not found.");

        try
        {
            var transaction = await _service.RechargeAsync(request.CustomerId, request.Amount, request.Reference, ct);
            return Ok(transaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public record DailyBillingRequest(Guid CustomerId, DateOnly Date, decimal RatePerKwh);

    /// <summary>
    /// Idempotent per (customer, date) — re-running the same day is a safe no-op. Returns 204 if
    /// there's no consumption data for that day yet (never fabricates a zero-consumption charge).
    /// </summary>
    [HttpPost("daily-billing")]
    public async Task<IActionResult> ProcessDailyBilling([FromBody] DailyBillingRequest request, CancellationToken ct)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists) return NotFound($"Customer {request.CustomerId} not found.");

        var transaction = await _service.ProcessDailyBillingAsync(request.CustomerId, request.Date, request.RatePerKwh, ct);
        return transaction is null ? NoContent() : Ok(transaction);
    }

    [HttpPost("accounts/{customerId:guid}/disconnect")]
    public async Task<IActionResult> Disconnect(Guid customerId, CancellationToken ct)
    {
        var account = await _db.PrepaidAccounts.FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);
        if (account is null) return NotFound();

        try
        {
            account.Disconnect();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(account);
    }

    [HttpPost("accounts/{customerId:guid}/reconnect")]
    public async Task<IActionResult> Reconnect(Guid customerId, CancellationToken ct)
    {
        var account = await _db.PrepaidAccounts.FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);
        if (account is null) return NotFound();

        try
        {
            account.Reconnect();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(account);
    }
}
