using MDMS.Domain.Common;

namespace MDMS.Domain.Entities;

/// <summary>
/// The meter's monthly Billing Profile (BP) — the commercial snapshot created at the start of
/// each billing cycle: cumulative import/export energy (kWh/kVAh), the average power factor over
/// the period, per-tariff-zone (TZ1-TZ8) kWh/kVAh splits, maximum demand (kW/kVA), and the
/// billing-period power-on duration. MDMS's job ends at supplying this snapshot — tariff
/// application, customer-category rules, and the actual bill amount are the downstream
/// billing/RMS system's responsibility, not this project's.
/// </summary>
public class BillingProfile : Entity
{
    public Guid MeterId { get; private set; }

    /// <summary>The billing cycle's start date (the meter creates BP at the start of the month).</summary>
    public DateOnly BillingDate { get; private set; }

    public decimal CumulativeKwhImport { get; private set; }
    public decimal CumulativeKvahImport { get; private set; }
    public decimal CumulativeKwhExport { get; private set; }
    public decimal CumulativeKvahExport { get; private set; }
    public decimal AveragePowerFactor { get; private set; }

    /// <summary>kWh for tariff zones TZ1..TZ8, in order (index 0 = TZ1). Always length 8.</summary>
    public decimal[] KwhByTariffZone { get; private set; } = new decimal[8];

    /// <summary>kVAh for tariff zones TZ1..TZ8, in order (index 0 = TZ1). Always length 8.</summary>
    public decimal[] KvahByTariffZone { get; private set; } = new decimal[8];

    public decimal MaximumDemandKw { get; private set; }
    public decimal MaximumDemandKva { get; private set; }
    public int BillingPowerOnDurationMinutes { get; private set; }

    private BillingProfile() { }

    public BillingProfile(
        Guid meterId, DateOnly billingDate,
        decimal cumulativeKwhImport, decimal cumulativeKvahImport, decimal cumulativeKwhExport, decimal cumulativeKvahExport,
        decimal averagePowerFactor, decimal[] kwhByTariffZone, decimal[] kvahByTariffZone,
        decimal maximumDemandKw, decimal maximumDemandKva, int billingPowerOnDurationMinutes)
    {
        if (kwhByTariffZone.Length != 8)
            throw new ArgumentException("kWh by tariff zone must have exactly 8 values (TZ1..TZ8).", nameof(kwhByTariffZone));
        if (kvahByTariffZone.Length != 8)
            throw new ArgumentException("kVAh by tariff zone must have exactly 8 values (TZ1..TZ8).", nameof(kvahByTariffZone));

        MeterId = meterId;
        BillingDate = billingDate;
        CumulativeKwhImport = cumulativeKwhImport;
        CumulativeKvahImport = cumulativeKvahImport;
        CumulativeKwhExport = cumulativeKwhExport;
        CumulativeKvahExport = cumulativeKvahExport;
        AveragePowerFactor = averagePowerFactor;
        KwhByTariffZone = kwhByTariffZone;
        KvahByTariffZone = kvahByTariffZone;
        MaximumDemandKw = maximumDemandKw;
        MaximumDemandKva = maximumDemandKva;
        BillingPowerOnDurationMinutes = billingPowerOnDurationMinutes;
    }
}
