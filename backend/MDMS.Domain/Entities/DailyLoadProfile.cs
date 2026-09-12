using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// The meter's authoritative Daily Load Profile (DLP), created at the 00:00 boundary. Mirrors
/// prepaid_engine's <c>DailyLoadProfile</c> shape (including the provisional/estimated-when-
/// missing concept) since DLP settlement math is expected to keep living in prepaid_engine —
/// MDMS's job is only to produce and validate the trusted daily figure it settles against.
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
    /// deadline — never silently treated as zero consumption. Matches prepaid_engine's demo
    /// estimation rule (average of up to the previous 7 valid DLPs, 0 if none exist), kept here
    /// so MDMS is the single place that rule is implemented once a real estimation service exists.
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

    /// <summary>Replaces a provisional profile once the real DLP arrives. Never mutates a Received profile.</summary>
    public DailyLoadProfile ReplaceWithReceived(decimal consumptionKwh)
    {
        if (Source == MeasurementSource.Received)
            throw new InvalidOperationException("A received DLP is never replaced.");

        return CreateReceived(ServicePointId, MeterId, ProfileDate, consumptionKwh);
    }
}
