using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// A single meter-reported event or alarm (power failure/restore, tamper, cover open, over/under
/// voltage, etc.) — see <see cref="MeterEventType"/>'s own doc comment for why alarms aren't a
/// separate entity. Acknowledgement is tracked explicitly so a real operational alarm can't be
/// silently lost: it's either acknowledged by a specific user at a specific time, or it isn't.
/// The Occ* fields are an optional snapshot of the electrical reading at the moment the
/// underlying condition was detected (e.g. the voltage that triggered an OverVoltage alarm) —
/// populated only when the ingesting caller actually provides one, never fabricated. Resolution
/// (the condition clearing) is tracked separately from acknowledgement (a human noticing it).
/// </summary>
public class MeterEvent : Entity
{
    public Guid MeterId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public MeterEventType EventType { get; private set; }
    public MeterEventSeverity Severity { get; private set; }
    public string? Description { get; private set; }

    public decimal? OccCurrent { get; private set; }
    public decimal? OccVoltage { get; private set; }
    public decimal? OccKwh { get; private set; }
    public decimal? OccTemperature { get; private set; }

    public DateTime? ResolvedAtUtc { get; private set; }

    public bool IsAcknowledged { get; private set; }
    public Guid? AcknowledgedByUserId { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }

    private MeterEvent() { }

    public MeterEvent(
        Guid meterId, DateTime occurredAtUtc, MeterEventType eventType, MeterEventSeverity severity, string? description,
        decimal? occCurrent = null, decimal? occVoltage = null, decimal? occKwh = null, decimal? occTemperature = null)
    {
        MeterId = meterId;
        OccurredAtUtc = occurredAtUtc;
        EventType = eventType;
        Severity = severity;
        Description = description;
        OccCurrent = occCurrent;
        OccVoltage = occVoltage;
        OccKwh = occKwh;
        OccTemperature = occTemperature;
    }

    /// <summary>The underlying condition cleared (e.g. voltage returned to normal) — distinct
    /// from a human acknowledging it. A resolved event can still be unacknowledged, and vice versa.</summary>
    public void Resolve(DateTime resolvedAtUtc)
    {
        if (ResolvedAtUtc is not null)
            throw new InvalidOperationException("This event is already resolved.");
        if (resolvedAtUtc < OccurredAtUtc)
            throw new ArgumentException("Resolution time cannot be before the occurrence time.", nameof(resolvedAtUtc));

        ResolvedAtUtc = resolvedAtUtc;
    }

    public void Acknowledge(Guid userId)
    {
        if (IsAcknowledged)
            throw new InvalidOperationException("This event is already acknowledged.");

        IsAcknowledged = true;
        AcknowledgedByUserId = userId;
        AcknowledgedAtUtc = DateTime.UtcNow;
    }
}
