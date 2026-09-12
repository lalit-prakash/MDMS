using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// One 30-minute Load Survey (LS) interval block for a meter, carrying explicit
/// <see cref="Source"/> provenance — distinguishing received/estimated/edited values is MDMS's
/// job, not any downstream billing system's.
/// </summary>
public class LoadSurveyInterval : Entity
{
    public Guid MeterId { get; private set; }

    public DateTime IntervalStartUtc { get; private set; }
    public DateTime IntervalEndUtc { get; private set; }

    /// <summary>Cumulative meter reading at <see cref="IntervalEndUtc"/>.</summary>
    public decimal CumulativeReading { get; private set; }

    /// <summary>Consumption for this interval alone (CumulativeReading − prior interval's CumulativeReading).</summary>
    public decimal ConsumptionKwh { get; private set; }

    public MeasurementQuality Quality { get; private set; }
    public MeasurementSource Source { get; private set; }

    private LoadSurveyInterval() { }

    /// <summary>
    /// Constructs a validated interval. Callers (the ingestion service) are responsible for
    /// running sequence-continuity/negative-consumption checks against the prior interval for
    /// the same meter before calling this — the entity itself only enforces internal consistency.
    /// </summary>
    public static LoadSurveyInterval CreateValid(
        Guid meterId, DateTime intervalStartUtc, DateTime intervalEndUtc,
        decimal cumulativeReading, decimal consumptionKwh, MeasurementSource source)
    {
        if (intervalEndUtc <= intervalStartUtc)
            throw new ArgumentException("Interval end must be after interval start.");

        return new LoadSurveyInterval
        {
            MeterId = meterId,
            IntervalStartUtc = intervalStartUtc,
            IntervalEndUtc = intervalEndUtc,
            CumulativeReading = cumulativeReading,
            ConsumptionKwh = consumptionKwh,
            Quality = MeasurementQuality.Valid,
            Source = source
        };
    }

    /// <summary>
    /// Flags an already-stored Valid interval as out of a configured plausibility range. Never
    /// changes <see cref="ConsumptionKwh"/> or <see cref="CumulativeReading"/> — only the quality
    /// annotation — so the underlying measurement stays intact for audit and a later re-check
    /// (e.g. after a threshold correction) is not comparing against an already-mutated value.
    /// </summary>
    public void FlagOutOfRange()
    {
        if (Quality != MeasurementQuality.Valid)
            throw new InvalidOperationException($"Only a Valid interval can be flagged out of range (was {Quality}).");

        Quality = MeasurementQuality.OutOfRange;
    }

    public static LoadSurveyInterval CreateRejected(
        Guid meterId, DateTime intervalStartUtc, DateTime intervalEndUtc,
        decimal cumulativeReading, MeasurementQuality quality)
    {
        if (quality == MeasurementQuality.Valid)
            throw new ArgumentException("A rejected interval cannot be marked Valid.", nameof(quality));

        return new LoadSurveyInterval
        {
            MeterId = meterId,
            IntervalStartUtc = intervalStartUtc,
            IntervalEndUtc = intervalEndUtc,
            CumulativeReading = cumulativeReading,
            ConsumptionKwh = 0,
            Quality = quality,
            Source = MeasurementSource.Received
        };
    }
}
