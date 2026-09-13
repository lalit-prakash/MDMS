using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// A single 15-minute Instantaneous Profile (IP) reading — the meter's point-in-time electrical
/// state (voltage/current/power factor/frequency/kW/kVA/kWh/kVAh), its running maximum-demand
/// markers (import and export, kW and kVA, each with the timestamp the peak occurred), and
/// device-health counters (power-on duration, tamper/billing/programming counts) plus its current
/// load-limit state. This is raw telemetry, not a measurement subject to VEE plausibility
/// checking — unlike Load Survey, there's no "expected range" for e.g. voltage that this project
/// currently defines, so no quality/estimation pipeline runs against IP yet.
/// </summary>
public class InstantaneousProfile : Entity
{
    public Guid MeterId { get; private set; }
    public DateTime MeterTimeUtc { get; private set; }

    public decimal Voltage { get; private set; }
    public decimal PhaseCurrent { get; private set; }
    public decimal NeutralCurrent { get; private set; }
    public decimal PowerFactor { get; private set; }
    public decimal Frequency { get; private set; }
    public decimal Kw { get; private set; }
    public decimal Kva { get; private set; }
    public decimal Kwh { get; private set; }
    public decimal Kvah { get; private set; }
    public decimal KwhExport { get; private set; }
    public decimal KvahExport { get; private set; }

    public decimal? MdKw { get; private set; }
    public DateTime? MdKwAtUtc { get; private set; }
    public decimal? MdKva { get; private set; }
    public DateTime? MdKvaAtUtc { get; private set; }
    public decimal? MdKwExport { get; private set; }
    public DateTime? MdKwExportAtUtc { get; private set; }
    public decimal? MdKvaExport { get; private set; }
    public DateTime? MdKvaExportAtUtc { get; private set; }

    public int PowerOnDurationMinutes { get; private set; }
    public int TamperCount { get; private set; }
    public int BillingCount { get; private set; }
    public int ProgrammingCount { get; private set; }

    public LoadLimitState LoadLimitState { get; private set; }
    public decimal? LoadLimitValue { get; private set; }

    private InstantaneousProfile() { }

    public InstantaneousProfile(
        Guid meterId, DateTime meterTimeUtc,
        decimal voltage, decimal phaseCurrent, decimal neutralCurrent, decimal powerFactor, decimal frequency,
        decimal kw, decimal kva, decimal kwh, decimal kvah, decimal kwhExport, decimal kvahExport,
        int powerOnDurationMinutes, int tamperCount, int billingCount, int programmingCount,
        LoadLimitState loadLimitState, decimal? loadLimitValue,
        decimal? mdKw = null, DateTime? mdKwAtUtc = null,
        decimal? mdKva = null, DateTime? mdKvaAtUtc = null,
        decimal? mdKwExport = null, DateTime? mdKwExportAtUtc = null,
        decimal? mdKvaExport = null, DateTime? mdKvaExportAtUtc = null)
    {
        MeterId = meterId;
        MeterTimeUtc = meterTimeUtc;
        Voltage = voltage;
        PhaseCurrent = phaseCurrent;
        NeutralCurrent = neutralCurrent;
        PowerFactor = powerFactor;
        Frequency = frequency;
        Kw = kw;
        Kva = kva;
        Kwh = kwh;
        Kvah = kvah;
        KwhExport = kwhExport;
        KvahExport = kvahExport;
        PowerOnDurationMinutes = powerOnDurationMinutes;
        TamperCount = tamperCount;
        BillingCount = billingCount;
        ProgrammingCount = programmingCount;
        LoadLimitState = loadLimitState;
        LoadLimitValue = loadLimitValue;
        MdKw = mdKw;
        MdKwAtUtc = mdKwAtUtc;
        MdKva = mdKva;
        MdKvaAtUtc = mdKvaAtUtc;
        MdKwExport = mdKwExport;
        MdKwExportAtUtc = mdKwExportAtUtc;
        MdKvaExport = mdKvaExport;
        MdKvaExportAtUtc = mdKvaExportAtUtc;
    }
}
