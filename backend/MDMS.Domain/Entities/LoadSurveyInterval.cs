using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// One 30-minute Load Survey (LS) interval block for a meter. Field shape mirrors
/// prepaid_engine's own <c>LoadSurveyInterval</c> deliberately, so exposing this as
/// validated data to prepaid_engine (replacing its current self-ingestion) is a near-direct
/// mapping. MDMS adds explicit <see cref="Source"/> provenance on top, since distinguishing
/// received/estimated/edited values is MDMS's job, not the billing engine's.
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
