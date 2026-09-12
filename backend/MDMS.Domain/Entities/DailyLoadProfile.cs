using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// The meter's authoritative Daily Load Profile (DLP), created at the 00:00 boundary, including a
/// provisional/estimated-when-missing path. MDMS's job is to produce and validate the trusted
/// daily figure — any downstream settlement/billing math against that figure is out of scope here.
/// </summary>
public class DailyLoadProfile : Entity
{
    public Guid ServicePointId { get; private set; }
    public Guid MeterId { get; private set; }
    public DateOnly ProfileDate { get; private set; }

    public decimal ConsumptionKwh { get; private set; }
    public MeasurementSource Source { get; private set; }
    public MeasurementQuality Quality { get; private set; }

    private DailyLoadProfile() { }

    public static DailyLoadProfile CreateReceived(
        Guid servicePointId, Guid meterId, DateOnly profileDate, decimal consumptionKwh)
        => new()
        {
            ServicePointId = servicePointId,
            MeterId = meterId,
            ProfileDate = profileDate,
            ConsumptionKwh = consumptionKwh,
            Source = MeasurementSource.Received,
            Quality = MeasurementQuality.Valid
        };

    /// <summary>
    /// A provisional profile substituted when no real DLP arrives by the daily processing
    /// deadline — never silently treated as zero consumption. The estimation rule itself (e.g.
    /// averaging recent valid DLPs) belongs in a real estimation service; this factory only
    /// records the outcome as explicitly provisional/estimated.
    /// </summary>
    public static DailyLoadProfile CreateProvisional(
        Guid servicePointId, Guid meterId, DateOnly profileDate, decimal estimatedConsumptionKwh)
        => new()
        {
            ServicePointId = servicePointId,
            MeterId = meterId,
            ProfileDate = profileDate,
            ConsumptionKwh = estimatedConsumptionKwh,
            Source = MeasurementSource.Estimated,
            Quality = MeasurementQuality.Missing
        };

    /// <summary>
    /// Flags an already-stored Valid profile as out of a configured plausibility range. Never
    /// changes <see cref="ConsumptionKwh"/> — only the quality annotation — mirroring
    /// <see cref="LoadSurveyInterval.FlagOutOfRange"/>.
    /// </summary>
    public void FlagOutOfRange()
    {
        if (Quality != MeasurementQuality.Valid)
            throw new InvalidOperationException($"Only a Valid profile can be flagged out of range (was {Quality}).");

        Quality = MeasurementQuality.OutOfRange;
    }

    /// <summary>Replaces a provisional profile once the real DLP arrives. Never mutates a Received profile.</summary>
    public DailyLoadProfile ReplaceWithReceived(decimal consumptionKwh)
    {
        if (Source == MeasurementSource.Received)
            throw new InvalidOperationException("A received DLP is never replaced.");

        return CreateReceived(ServicePointId, MeterId, ProfileDate, consumptionKwh);
    }
}
