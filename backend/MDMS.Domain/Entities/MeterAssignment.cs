using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// An audit record binding a <see cref="Meter"/> to a <see cref="ServicePoint"/> for a span of
/// time, and — on replacement — the closing/opening readings that make the boundary explicit.
/// Mirrors prepaid_engine's <c>MeterAssignment</c>: the entire point of this record is that an
/// old meter's cumulative reading is never compared against a new meter's, because every
/// measurement is scoped to a specific <see cref="MeterId"/> and every assignment scoped to a
/// specific time range.
/// </summary>
public class MeterAssignment : Entity
{
    public Guid ServicePointId { get; private set; }
    public Guid MeterId { get; private set; }
    public Meter? Meter { get; private set; }

    public MeterAssignmentEventType EventType { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }

    /// <summary>Closing cumulative reading of the meter being replaced/removed, if applicable.</summary>
    public decimal? ClosingReading { get; private set; }

    /// <summary>Opening cumulative reading of the newly installed meter, if applicable.</summary>
    public decimal? OpeningReading { get; private set; }

    public string? Reason { get; private set; }

    private MeterAssignment() { }

    public static MeterAssignment CreateInstallation(
        Guid servicePointId, Guid meterId, DateTime effectiveFromUtc, decimal? openingReading, string? reason)
        => new()
        {
            ServicePointId = servicePointId,
            MeterId = meterId,
            EventType = MeterAssignmentEventType.InitialInstallation,
            EffectiveFromUtc = effectiveFromUtc,
            OpeningReading = openingReading,
            Reason = reason
        };

    public static MeterAssignment CreateReplacement(
        Guid servicePointId, Guid newMeterId, DateTime effectiveFromUtc,
        decimal closingReadingOfOldMeter, decimal openingReadingOfNewMeter, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A replacement must record a reason.", nameof(reason));

        return new MeterAssignment
        {
            ServicePointId = servicePointId,
            MeterId = newMeterId,
            EventType = MeterAssignmentEventType.Replacement,
            EffectiveFromUtc = effectiveFromUtc,
            ClosingReading = closingReadingOfOldMeter,
            OpeningReading = openingReadingOfNewMeter,
            Reason = reason
        };
    }

    public void Close(DateTime effectiveToUtc, decimal closingReading)
    {
        EffectiveToUtc = effectiveToUtc;
        ClosingReading = closingReading;
    }
}
