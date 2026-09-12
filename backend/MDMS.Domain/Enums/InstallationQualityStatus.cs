namespace MDMS.Domain.Enums;

/// <summary>
/// The project's stated three-level installation quality check workflow. A rejection at L2
/// returns to L1_Pending; a rejection at L3 returns to L2_Pending — never all the way back to
/// Draft, since the point of a rejection is to re-do the specific level's work, not restart
/// the whole installation.
/// </summary>
public enum InstallationQualityStatus
{
    Draft = 1,
    L1Pending = 2,
    L1Completed = 3,
    L2Pending = 4,
    L2Approved = 5,
    L3Pending = 6,
    L3Approved = 7,
    RmsSyncPending = 8,
    RmsSynced = 9,
    MisOnboarded = 10
}
