using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// One node of the organizational hierarchy — Zone → Circle → Division → Sub Division → Section —
/// used to scope a <see cref="User"/>'s access (field roles are constrained by geography, not
/// only by role) and to organize dashboards/reports/work assignment. A single self-referencing
/// tree, same pattern as <see cref="HierarchyNode"/> but for a distinct purpose — the two
/// hierarchies never share levels or nodes.
/// </summary>
public class OrgUnit : Entity
{
    public OrgUnitType UnitType { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Guid? ParentId { get; private set; }
    public OrgUnit? Parent { get; private set; }

    private OrgUnit() { }

    /// <summary>A top-level <see cref="OrgUnitType.Region"/> node has no parent. Optional: a Zone may
    /// also stand alone, for a DISCOM whose hierarchy starts at Zone.</summary>
    public static OrgUnit CreateRegion(string code, string name)
        => new()
        {
            UnitType = OrgUnitType.Region,
            Code = RequireCode(code),
            Name = RequireName(name),
            ParentId = null
        };

    /// <summary>A <see cref="OrgUnitType.Zone"/> created this way has no parent (use
    /// <see cref="CreateChild"/> to put a Zone under a Region).</summary>
    public static OrgUnit CreateZone(string code, string name)
        => new()
        {
            UnitType = OrgUnitType.Zone,
            Code = RequireCode(code),
            Name = RequireName(name),
            ParentId = null
        };

    /// <summary>
    /// Every non-Zone unit must nest directly under a parent exactly one level coarser — never
    /// skipping a level — so access scoping and drill-down reports can always assume a consistent depth.
    /// </summary>
    public static OrgUnit CreateChild(OrgUnitType unitType, OrgUnit parent, string code, string name)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var expectedParentType = unitType switch
        {
            OrgUnitType.Zone => OrgUnitType.Region,
            OrgUnitType.Circle => OrgUnitType.Zone,
            OrgUnitType.Division => OrgUnitType.Circle,
            OrgUnitType.SubDivision => OrgUnitType.Division,
            OrgUnitType.Section => OrgUnitType.SubDivision,
            OrgUnitType.Region => throw new ArgumentException(
                "A Region has no parent; use CreateRegion instead.", nameof(unitType)),
            _ => throw new ArgumentOutOfRangeException(nameof(unitType))
        };

        if (parent.UnitType != expectedParentType)
        {
            throw new ArgumentException(
                $"A {unitType} must be created under a {expectedParentType}, not a {parent.UnitType}.",
                nameof(parent));
        }

        return new OrgUnit
        {
            UnitType = unitType,
            Code = RequireCode(code),
            Name = RequireName(name),
            ParentId = parent.Id
        };
    }

    private static string RequireCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        return code;
    }

    private static string RequireName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        return name;
    }
}
