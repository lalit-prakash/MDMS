using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// A consumer complaint tied to a customer (and, where relevant, a specific meter), tracked
/// through Open → Assigned → InProgress → Resolved → Closed. Closing requires having gone through
/// Resolved first — "NOMC validation" per the spec's flow — so a field action can never
/// self-close a ticket without a separate confirmation step.
/// </summary>
public class Complaint : Entity
{
    public Guid CustomerId { get; private set; }
    public Guid? MeterId { get; private set; }
    public ComplaintSource Source { get; private set; }
    public string Description { get; private set; } = default!;
    public ComplaintStatus Status { get; private set; }
    public DateTime SlaDueUtc { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public string? ResolutionNote { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    private Complaint() { }

    public Complaint(Guid customerId, Guid? meterId, ComplaintSource source, string description, TimeSpan slaDuration)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (slaDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(slaDuration), "SLA duration must be positive.");

        CustomerId = customerId;
        MeterId = meterId;
        Source = source;
        Description = description;
        Status = ComplaintStatus.Open;
        SlaDueUtc = CreatedAtUtc + slaDuration;
    }

    public void AssignTo(Guid userId)
    {
        if (Status is ComplaintStatus.Resolved or ComplaintStatus.Closed)
            throw new InvalidOperationException($"Cannot assign a {Status} complaint.");

        AssignedToUserId = userId;
        Status = ComplaintStatus.Assigned;
    }

    public void StartProgress()
    {
        if (Status != ComplaintStatus.Assigned)
            throw new InvalidOperationException($"Only an Assigned complaint can start progress (was {Status}).");

        Status = ComplaintStatus.InProgress;
    }

    public void Resolve(string resolutionNote)
    {
        if (Status is ComplaintStatus.Closed)
            throw new InvalidOperationException("A Closed complaint cannot be resolved again.");
        if (string.IsNullOrWhiteSpace(resolutionNote))
            throw new ArgumentException("A resolution note is required.", nameof(resolutionNote));

        ResolutionNote = resolutionNote;
        ResolvedAtUtc = DateTime.UtcNow;
        Status = ComplaintStatus.Resolved;
    }

    /// <summary>The final confirmation step ("NOMC validation") — only reachable from Resolved.</summary>
    public void Close()
    {
        if (Status != ComplaintStatus.Resolved)
            throw new InvalidOperationException($"Only a Resolved complaint can be closed (was {Status}) — resolve it first.");

        Status = ComplaintStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
    }

    public bool IsOverdue(DateTime nowUtc) => Status != ComplaintStatus.Closed && nowUtc > SlaDueUtc;
}
