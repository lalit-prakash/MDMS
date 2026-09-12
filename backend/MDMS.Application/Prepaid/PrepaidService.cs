using MDMS.Application.Common;
using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Application.Prepaid;

/// <summary>
/// Recharge and daily-billing orchestration for prepaid accounts. Both operations are idempotent
/// on a caller-supplied reference — replaying the same reference (a duplicate payment-gateway
/// callback, or re-running a billing job for a day already processed) returns the original
/// transaction rather than double-crediting or double-debiting the wallet.
/// </summary>
public class PrepaidService
{
    private readonly IMdmsDbContext _db;

    public PrepaidService(IMdmsDbContext db)
    {
        _db = db;
    }

    public async Task<PrepaidAccount> GetOrOpenAccountAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var account = await _db.PrepaidAccounts.FirstOrDefaultAsync(a => a.CustomerId == customerId, cancellationToken);
        if (account is not null)
            return account;

        account = PrepaidAccount.Open(customerId);
        _db.PrepaidAccounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account;
    }

    /// <summary>Idempotent on <paramref name="reference"/>: a repeat call with the same reference returns the original transaction, never a second credit.</summary>
    public async Task<WalletTransaction> RechargeAsync(
        Guid customerId, decimal amount, string reference, CancellationToken cancellationToken = default)
    {
        var existing = await _db.WalletTransactions.FirstOrDefaultAsync(t => t.Reference == reference, cancellationToken);
        if (existing is not null)
            return existing;

        var account = await GetOrOpenAccountAsync(customerId, cancellationToken);
        var transaction = account.Recharge(amount, reference);

        _db.WalletTransactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    /// <summary>
    /// Charges a customer's account for one day's validated consumption across every service
    /// point linked to them, at a flat caller-supplied rate. Idempotent via a reference derived
    /// from (customer, date) — re-running the same day's billing is a safe no-op. This is a
    /// deliberately minimal placeholder tariff calculation (flat rate × kWh, no slabs/categories/
    /// ToD) — real tariff computation is out of this project's current scope; see the README.
    /// </summary>
    public async Task<WalletTransaction?> ProcessDailyBillingAsync(
        Guid customerId, DateOnly date, decimal ratePerKwh, CancellationToken cancellationToken = default)
    {
        var reference = $"DAILY-{customerId:N}-{date:yyyy-MM-dd}";
        var existing = await _db.WalletTransactions.FirstOrDefaultAsync(t => t.Reference == reference, cancellationToken);
        if (existing is not null)
            return existing;

        var servicePointIds = await _db.ServicePoints
            .Where(sp => sp.CustomerId == customerId)
            .Select(sp => sp.Id)
            .ToListAsync(cancellationToken);

        var consumptionKwh = await _db.DailyLoadProfiles
            .Where(p => servicePointIds.Contains(p.ServicePointId) && p.ProfileDate == date)
            .SumAsync(p => (decimal?)p.ConsumptionKwh, cancellationToken) ?? 0m;

        if (consumptionKwh == 0m)
            return null; // nothing to bill — never fabricate a charge for a day with no consumption data.

        var charge = consumptionKwh * ratePerKwh;
        var account = await GetOrOpenAccountAsync(customerId, cancellationToken);
        var transaction = account.DebitForConsumption(charge, reference, $"Daily billing for {date:yyyy-MM-dd}: {consumptionKwh} kWh @ {ratePerKwh}/kWh.");

        _db.WalletTransactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);
        return transaction;
    }
}
