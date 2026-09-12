namespace MDMS.Domain.Enums;

/// <summary>
/// A revenue-protection lead's workflow, per the spec: Detected → Scored → Reviewed → Assigned →
/// FieldInvestigation → FindingRecorded → ActionTaken → RecoveryRecorded → Closed. This is an
/// investigation/lead-generation pipeline, not a legal conclusion — see
/// <see cref="Entities.RevenueProtectionLead"/>.
/// </summary>
public enum RevenueProtectionLeadStatus
{
    Detected = 1,
    Scored = 2,
    Reviewed = 3,
    Assigned = 4,
    FieldInvestigation = 5,
    FindingRecorded = 6,
    ActionTaken = 7,
    RecoveryRecorded = 8,
    Closed = 9
}
