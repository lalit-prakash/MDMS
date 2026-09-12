using MDMS.Domain.Common;

namespace MDMS.Domain.Entities;

/// <summary>
/// The account/customer that a <see cref="ServicePoint"/> (and, through it, a meter) belongs to.
/// MDMS owns this as master data; it is deliberately a thin record — billing-relevant facts
/// (tariff category, wallet, connection status) are downstream billing systems' responsibility,
/// not MDMS's, and should reference this record by <see cref="AccountNumber"/> rather than
/// duplicating it.
/// </summary>
public class Customer : Entity
{
    /// <summary>The utility's own account/consumer number — the key downstream billing systems reference this record by.</summary>
    public string AccountNumber { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    private Customer() { }

    public Customer(string accountNumber, string name)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("Account number is required.", nameof(accountNumber));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        AccountNumber = accountNumber;
        Name = name;
    }
}
