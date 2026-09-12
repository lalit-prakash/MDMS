using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// The three-level installation quality check for one meter installation: Contractor (L1) →
/// Quality Incharge (L2) → Utility Manager (L3) → RMS master sync → MIS onboarding. Every
/// rejected record preserves the rejection level implicitly (its <see cref="Status"/> returns to
/// the prior pending level) plus who rejected it and why, so nothing about a correction cycle is lost.
/// </summary>
public class InstallationQualityCheck : Entity
{
    public Guid MeterId { get; private set; }
    public InstallationQualityStatus Status { get; private set; }

    public Guid? L1CompletedByUserId { get; private set; }
    public DateTime? L1CompletedAtUtc { get; private set; }

    public Guid? L2DecisionByUserId { get; private set; }
    public DateTime? L2DecisionAtUtc { get; private set; }
    public string? L2RejectionNote { get; private set; }

    public Guid? L3DecisionByUserId { get; private set; }
    public DateTime? L3DecisionAtUtc { get; private set; }
    public string? L3RejectionNote { get; private set; }

    private InstallationQualityCheck() { }

    public static InstallationQualityCheck Create(Guid meterId)
        => new() { MeterId = meterId, Status = InstallationQualityStatus.Draft };

    public void StartL1()
    {
        RequireStatus(InstallationQualityStatus.Draft);
        Status = InstallationQualityStatus.L1Pending;
    }

    public void CompleteL1(Guid userId)
    {
        RequireStatus(InstallationQualityStatus.L1Pending);
        L1CompletedByUserId = userId;
        L1CompletedAtUtc = DateTime.UtcNow;
        Status = InstallationQualityStatus.L1Completed;
    }

    public void SubmitToL2()
    {
        RequireStatus(InstallationQualityStatus.L1Completed);
        Status = InstallationQualityStatus.L2Pending;
    }

    public void ApproveL2(Guid userId)
    {
        RequireStatus(InstallationQualityStatus.L2Pending);
        L2DecisionByUserId = userId;
        L2DecisionAtUtc = DateTime.UtcNow;
        L2RejectionNote = null;
        Status = InstallationQualityStatus.L2Approved;
    }

    /// <summary>Rejects back to L1 — the contractor's own work must be redone, not the whole installation.</summary>
    public void RejectL2(Guid userId, string note)
    {
        RequireStatus(InstallationQualityStatus.L2Pending);
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("A rejection requires a note.", nameof(note));

        L2DecisionByUserId = userId;
        L2DecisionAtUtc = DateTime.UtcNow;
        L2RejectionNote = note;
        Status = InstallationQualityStatus.L1Pending;
    }

    public void SubmitToL3()
    {
        RequireStatus(InstallationQualityStatus.L2Approved);
        Status = InstallationQualityStatus.L3Pending;
    }

    public void ApproveL3(Guid userId)
    {
        RequireStatus(InstallationQualityStatus.L3Pending);
        L3DecisionByUserId = userId;
        L3DecisionAtUtc = DateTime.UtcNow;
        L3RejectionNote = null;
        Status = InstallationQualityStatus.L3Approved;
    }

    /// <summary>Rejects back to L2 — the quality-incharge review must be redone, not L1's field work.</summary>
    public void RejectL3(Guid userId, string note)
    {
        RequireStatus(InstallationQualityStatus.L3Pending);
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("A rejection requires a note.", nameof(note));

        L3DecisionByUserId = userId;
        L3DecisionAtUtc = DateTime.UtcNow;
        L3RejectionNote = note;
        Status = InstallationQualityStatus.L2Pending;
    }

    public void MarkRmsSyncPending()
    {
        RequireStatus(InstallationQualityStatus.L3Approved);
        Status = InstallationQualityStatus.RmsSyncPending;
    }

    public void MarkRmsSynced()
    {
        RequireStatus(InstallationQualityStatus.RmsSyncPending);
        Status = InstallationQualityStatus.RmsSynced;
    }

    public void MarkMisOnboarded()
    {
        RequireStatus(InstallationQualityStatus.RmsSynced);
        Status = InstallationQualityStatus.MisOnboarded;
    }

    private void RequireStatus(InstallationQualityStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Expected status {expected} but was {Status}.");
    }
}
