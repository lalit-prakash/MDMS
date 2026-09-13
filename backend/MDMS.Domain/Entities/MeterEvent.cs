using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// A single meter-reported event or alarm (power failure/restore, tamper, cover open, over/under
/// voltage, etc.) — see <see cref="MeterEventType"/>'s own doc comment for why alarms aren't a
/// separate entity. Acknowledgement is tracked explicitly so a real operational alarm can't be
/// silently lost: it's either acknowledged by a specific user at a specific time, or it isn't.
/// </summary>
public class MeterEvent : Entity
{
    public Guid MeterId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public MeterEventType EventType { get; private set; }
    public MeterEventSeverity Severity { get; private set; }
    public string? Description { get; private set; }

    public bool IsAcknowledged { get; private set; }
    public Guid? AcknowledgedByUserId { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }

    private MeterEvent() { }

    public MeterEvent(Guid meterId, DateTime occurredAtUtc, MeterEventType eventType, MeterEventSeverity severity, string? description)
    {
        MeterId = meterId;
        OccurredAtUtc = occurredAtUtc;
        EventType = eventType;
        Severity = severity;
        Description = description;
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
