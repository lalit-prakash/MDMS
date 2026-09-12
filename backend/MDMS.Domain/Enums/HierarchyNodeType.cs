namespace MDMS.Domain.Enums;

/// <summary>
/// A level in the DISCOM's **electrical** hierarchy, used for energy-audit aggregation (comparing
/// energy entering a network level against energy accounted for downstream). Deliberately
/// separate from <see cref="OrgUnitType"/> (the organizational/RBAC hierarchy) — the two serve
/// different purposes and don't share levels: this one is Substation → Feeder → Distribution
/// Transformer, ending where a <see cref="Entities.ServicePoint"/> (the consumer) attaches.
/// </summary>
public enum HierarchyNodeType
{
    Substation = 1,
    Feeder = 2,
    DistributionTransformer = 3
}
