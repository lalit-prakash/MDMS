namespace MDMS.Domain.Enums;

/// <summary>A prepaid wallet ledger entry's kind — explicit, never inferred from the sign of the amount alone.</summary>
public enum WalletTransactionType
{
    Recharge = 1,
    ConsumptionDebit = 2,
    Adjustment = 3
}
