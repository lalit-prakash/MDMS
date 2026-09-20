namespace MDMS.Domain.Enums;

/// <summary>
/// A level in the DISCOM's **organizational** hierarchy, used for RBAC scoping, dashboards,
/// reports, and work assignment. Deliberately separate from <see cref="HierarchyNodeType"/> (the
/// electrical hierarchy used for energy-audit aggregation) — an org unit is an administrative
/// boundary a user is scoped to, not an electrical network node energy flows through.
/// </summary>
public enum OrgUnitType
{
    Region = 0,
    Zone = 1,
    Circle = 2,
    Division = 3,
    SubDivision = 4,
    Section = 5
}
