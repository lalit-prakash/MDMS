using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// One immutable, append-only entry in a <see cref="PrepaidAccount"/>'s ledger. Per the spec's
/// "prepaid must be ledger-based" principle, a balance is never a bare mutable number — every
/// change is this kind of row, carrying the balance it produced (<see cref="BalanceAfter"/>) so
/// history is self-verifying without recomputing from scratch. <see cref="Reference"/> is the
/// idempotency key (a payment gateway transaction ID, or a deterministic key for a daily billing
/// run) — a caller is expected to check for an existing transaction with the same reference
/// before creating a new one (see <c>PrepaidService</c>).
/// </summary>
public class WalletTransaction : Entity
{
    public Guid PrepaidAccountId { get; private set; }
    public WalletTransactionType Type { get; private set; }

    /// <summary>Signed: positive for a credit (Recharge), negative for a debit (ConsumptionDebit).</summary>
    public decimal Amount { get; private set; }

    public decimal BalanceAfter { get; private set; }
    public string Reference { get; private set; } = default!;
    public string? Note { get; private set; }

    private WalletTransaction() { }

    internal static WalletTransaction Create(
        Guid prepaidAccountId, WalletTransactionType type, decimal amount, decimal balanceAfter,
        string reference, string? note)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("A wallet transaction requires an idempotency reference.", nameof(reference));

        return new WalletTransaction
        {
            PrepaidAccountId = prepaidAccountId,
            Type = type,
            Amount = amount,
            BalanceAfter = balanceAfter,
            Reference = reference,
            Note = note
        };
    }
}
