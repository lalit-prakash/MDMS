namespace MDMS.Domain.Enums;

/// <summary>
/// The fixed set of roles this MDMS defines. Access is not role-only — a <see cref="Entities.User"/>
/// is also scoped to an <see cref="OrgUnit"/> (most field roles are constrained to their assigned
/// division/section, not the whole DISCOM). No permission-checking middleware exists yet; this
/// enum only records which role a user has been assigned — see the project notes for the pending
/// auth-package decision that blocks enforcing it.
/// </summary>
public enum UserRole
{
    Admin = 1,
    ItManager = 2,
    Nomc = 3,
    Supervisor = 4,
    QualityIncharge = 5,
    OmSupervisor = 6,
    Installer = 7,
    OmExecutive = 8,
    Contractor = 9,
    UtilityManager = 10,

    /// <summary>The consumer complaint helpline role (referred to as "1912" in the source requirements).</summary>
    ComplaintDesk = 11,

    StoreManager = 12
}
