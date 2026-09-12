using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// An investigation lead for possible revenue leakage — never a legal conclusion. Tracks
/// Detected → Scored → Reviewed → Assigned → FieldInvestigation → FindingRecorded → ActionTaken
/// → (optionally) RecoveryRecorded → Closed, per the spec. A lead accumulates
/// <see cref="RiskSignal"/>s that raise <see cref="RiskScore"/>; closing is allowed either after
/// a recorded recovery or directly from an action (e.g. a field visit that turns out to be a
/// false positive still needs a documented closure, not a forced recovery amount).
/// </summary>
public class RevenueProtectionLead : Entity
{
    public Guid CustomerId { get; private set; }
    public Guid? MeterId { get; private set; }
    public RevenueProtectionLeadStatus Status { get; private set; }
    public decimal RiskScore { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public string? FieldFindingNote { get; private set; }
    public string? ActionTaken { get; private set; }
    public decimal? RecoveryAmount { get; private set; }
    public string? ClosureReason { get; private set; }

    private RevenueProtectionLead() { }

    public static RevenueProtectionLead Detect(Guid customerId, Guid? meterId)
        => new() { CustomerId = customerId, MeterId = meterId, Status = RevenueProtectionLeadStatus.Detected, RiskScore = 0m };

    /// <summary>Adds a signal's weight to the running score. The first signal moves the lead from Detected to Scored.</summary>
    public void AddScore(decimal weight)
    {
        if (Status == RevenueProtectionLeadStatus.Closed)
            throw new InvalidOperationException("Cannot add a signal to a Closed lead.");

        RiskScore += weight;
        if (Status == RevenueProtectionLeadStatus.Detected)
            Status = RevenueProtectionLeadStatus.Scored;
    }

    public void Review()
    {
        RequireStatus(RevenueProtectionLeadStatus.Scored);
        Status = RevenueProtectionLeadStatus.Reviewed;
    }

    public void AssignTo(Guid userId)
    {
        RequireStatus(RevenueProtectionLeadStatus.Reviewed);
        AssignedToUserId = userId;
        Status = RevenueProtectionLeadStatus.Assigned;
    }

    public void StartFieldInvestigation()
    {
        RequireStatus(RevenueProtectionLeadStatus.Assigned);
        Status = RevenueProtectionLeadStatus.FieldInvestigation;
    }

    public void RecordFinding(string note)
    {
        RequireStatus(RevenueProtectionLeadStatus.FieldInvestigation);
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("A field finding note is required.", nameof(note));

        FieldFindingNote = note;
        Status = RevenueProtectionLeadStatus.FindingRecorded;
    }

    public void RecordAction(string action)
    {
        RequireStatus(RevenueProtectionLeadStatus.FindingRecorded);
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("An action is required.", nameof(action));

        ActionTaken = action;
        Status = RevenueProtectionLeadStatus.ActionTaken;
    }

    public void RecordRecovery(decimal amount)
    {
        RequireStatus(RevenueProtectionLeadStatus.ActionTaken);
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Recovery amount cannot be negative.");

        RecoveryAmount = amount;
        Status = RevenueProtectionLeadStatus.RecoveryRecorded;
    }

    /// <summary>Closeable from ActionTaken directly (e.g. a false positive with nothing to recover) or from RecoveryRecorded.</summary>
    public void Close(string reason)
    {
        if (Status is not (RevenueProtectionLeadStatus.ActionTaken or RevenueProtectionLeadStatus.RecoveryRecorded))
            throw new InvalidOperationException($"Cannot close a lead from status {Status}.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A closure reason is required.", nameof(reason));

        ClosureReason = reason;
        Status = RevenueProtectionLeadStatus.Closed;
    }

    private void RequireStatus(RevenueProtectionLeadStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Expected status {expected} but was {Status}.");
    }
}
