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

    /// <summary>
    /// The remaining consumer master-data fields from the reference Consumer Master Info sheet —
    /// all nullable and set only when actually supplied, via <see cref="SetMasterData"/>, never
    /// fabricated. Billing-computation facts (bill amount, arrears) still stay out of scope per
    /// this type's own doc comment; these are the descriptive/master fields only.
    /// </summary>
    public string? RrNumber { get; private set; }
    public string? MobileNumber { get; private set; }

    /// <summary>Consumer mobile-app password, set via ConsumerAuthController's register/reset
    /// flows (PBKDF2 hash, see PasswordHasher) — null until the consumer registers for password
    /// login. Independent of mobile-number-based login, which always remains available.</summary>
    public string? PasswordHash { get; private set; }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        PasswordHash = passwordHash;
    }

    public string? ConnectionStatus { get; private set; }
    public DateOnly? ServiceDate { get; private set; }
    public decimal? SanctionedLoadKw { get; private set; }
    public decimal? ContractDemandKva { get; private set; }
    public decimal? ConnectedLoadKw { get; private set; }
    public string? LoadType { get; private set; }
    public string? TariffCategoryCode { get; private set; }
    public string? CommunicationType { get; private set; }
    public string? PaymentMode { get; private set; }
    public bool? IsNetMeter { get; private set; }
    public int? BillDay { get; private set; }
    public string? BillCycle { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }

    /// <summary>Meter-related consumer master fields from the Consumer sheet (meter make/phase/MF,
    /// MR flag, satno, asset timestamp, replacement date) � set via <see cref="SetMeterAssetData"/>.</summary>
    public string? MeterMake { get; private set; }
    public string? MeterPhase { get; private set; }
    public decimal? MultiplyingFactor { get; private set; }
    public bool? IsMrRequiredDone { get; private set; }
    public int? Satno { get; private set; }
    public DateTime? MdmAssetTimestampUtc { get; private set; }
    public DateOnly? MeterReplacementDate { get; private set; }

    public void SetMeterAssetData(
        string? meterMake, string? meterPhase, decimal? multiplyingFactor, bool? isMrRequiredDone,
        int? satno, DateTime? mdmAssetTimestampUtc, DateOnly? meterReplacementDate)
    {
        MeterMake = meterMake; MeterPhase = meterPhase; MultiplyingFactor = multiplyingFactor;
        IsMrRequiredDone = isMrRequiredDone; Satno = satno; MdmAssetTimestampUtc = mdmAssetTimestampUtc;
        MeterReplacementDate = meterReplacementDate;
    }

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

    /// <summary>Sets the optional master-data fields. Safe to call any time — never fabricated by
    /// this project, only ever what an actual caller (import, onboarding form) supplies.</summary>
    public void SetMasterData(
        string? rrNumber, string? mobileNumber, string? connectionStatus, DateOnly? serviceDate,
        decimal? sanctionedLoadKw, decimal? contractDemandKva, decimal? connectedLoadKw,
        string? loadType, string? tariffCategoryCode, string? communicationType, string? paymentMode,
        bool? isNetMeter, int? billDay, string? billCycle, decimal? latitude, decimal? longitude)
    {
        RrNumber = rrNumber;
        MobileNumber = mobileNumber;
        ConnectionStatus = connectionStatus;
        ServiceDate = serviceDate;
        SanctionedLoadKw = sanctionedLoadKw;
        ContractDemandKva = contractDemandKva;
        ConnectedLoadKw = connectedLoadKw;
        LoadType = loadType;
        TariffCategoryCode = tariffCategoryCode;
        CommunicationType = communicationType;
        PaymentMode = paymentMode;
        IsNetMeter = isNetMeter;
        BillDay = billDay;
        BillCycle = billCycle;
        Latitude = latitude;
        Longitude = longitude;
    }
}
