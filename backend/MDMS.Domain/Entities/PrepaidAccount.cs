using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// A customer's prepaid wallet. The balance is a denormalized read of the ledger
/// (<see cref="WalletTransaction"/>) it produces — every mutation goes through a method here that
/// also returns the transaction row recording it, per the spec's ledger-based-wallet principle.
/// Reconnection requires a positive balance, checked here before the caller ever dispatches
/// anything — this project has no HES/meter command integration, so "reconnect" only means
/// flipping this account's own status, not confirming a physical meter action.
/// </summary>
public class PrepaidAccount : Entity
{
    public Guid CustomerId { get; private set; }
    public decimal Balance { get; private set; }
    public bool IsConnected { get; private set; }

    private PrepaidAccount() { }

    public static PrepaidAccount Open(Guid customerId)
        => new() { CustomerId = customerId, Balance = 0m, IsConnected = true };

    public WalletTransaction Recharge(decimal amount, string reference)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Recharge amount must be positive.");

        Balance += amount;
        return WalletTransaction.Create(Id, WalletTransactionType.Recharge, amount, Balance, reference, note: null);
    }

    /// <summary>
    /// Debits the wallet for a period's validated consumption charge. Never rejects on
    /// insufficient balance — the spec allows the balance to go negative/overdraft where utility
    /// rules permit; enforcing a floor is a policy decision for a caller/config to make, not this
    /// entity's job.
    /// </summary>
    public WalletTransaction DebitForConsumption(decimal amount, string reference, string? note)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Consumption charge cannot be negative.");

        Balance -= amount;
        return WalletTransaction.Create(Id, WalletTransactionType.ConsumptionDebit, -amount, Balance, reference, note);
    }

    public WalletTransaction Adjust(decimal signedAmount, string reference, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("A manual adjustment requires a note.", nameof(note));

        Balance += signedAmount;
        return WalletTransaction.Create(Id, WalletTransactionType.Adjustment, signedAmount, Balance, reference, note);
    }

    public void Disconnect()
    {
        if (!IsConnected)
            throw new InvalidOperationException("Account is already disconnected.");

        IsConnected = false;
    }

    public void Reconnect()
    {
        if (IsConnected)
            throw new InvalidOperationException("Account is already connected.");
        if (Balance <= 0)
            throw new InvalidOperationException("Cannot reconnect an account with a non-positive balance.");

        IsConnected = true;
    }
}
