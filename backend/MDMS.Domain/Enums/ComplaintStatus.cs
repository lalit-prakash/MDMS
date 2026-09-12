namespace MDMS.Domain.Enums;

/// <summary>
/// A complaint's lifecycle: Open (just raised) → Assigned (routed to a field role) → InProgress
/// (field action underway) → Resolved (fix applied, pending confirmation) → Closed (confirmed
/// done). Mirrors the spec's flow: Consumer → NOMC → resolve directly OR assign → O&M Supervisor
/// → O&M Executive → field action → NOMC validation → closure.
/// </summary>
public enum ComplaintStatus
{
    Open = 1,
    Assigned = 2,
    InProgress = 3,
    Resolved = 4,
    Closed = 5
}
