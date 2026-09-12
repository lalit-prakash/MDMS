using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// One node of the **electrical** hierarchy — Substation → Feeder → Distribution Transformer —
/// that energy-audit aggregation (and its real-loss vs. coverage-loss split) rolls up through. A
/// <see cref="ServicePoint"/> attaches to a DT node to complete the chain down to the consumer.
/// A single self-referencing tree rather than three separate tables — the three levels share
/// every behavior (name, code, one parent, many children) and nothing here needs level-specific
/// fields yet; if one later does, that level becomes its own subclass rather than bloating this
/// type. Not to be confused with <see cref="OrgUnit"/>, the separate organizational/RBAC
/// hierarchy (Zone/Circle/Division/Sub Division/Section) — the two never share levels or nodes.
/// </summary>
public class HierarchyNode : Entity
{
    public HierarchyNodeType NodeType { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Guid? ParentId { get; private set; }
    public HierarchyNode? Parent { get; private set; }

    private HierarchyNode() { }

    /// <summary>A top-level <see cref="HierarchyNodeType.Substation"/> node has no parent.</summary>
    public static HierarchyNode CreateSubstation(string code, string name)
        => new()
        {
            NodeType = HierarchyNodeType.Substation,
            Code = RequireCode(code),
            Name = RequireName(name),
            ParentId = null
        };

    /// <summary>
    /// Every non-Substation node must nest directly under a parent exactly one level coarser
    /// (Feeder under Substation, DT under Feeder) — never skipping a level and never attaching to
    /// a same-or-finer-level parent, so a hierarchy drill-down can always assume a consistent depth.
    /// </summary>
    public static HierarchyNode CreateChild(HierarchyNodeType nodeType, HierarchyNode parent, string code, string name)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var expectedParentType = nodeType switch
        {
            HierarchyNodeType.Feeder => HierarchyNodeType.Substation,
            HierarchyNodeType.DistributionTransformer => HierarchyNodeType.Feeder,
            HierarchyNodeType.Substation => throw new ArgumentException(
                "A Substation has no parent; use CreateSubstation instead.", nameof(nodeType)),
            _ => throw new ArgumentOutOfRangeException(nameof(nodeType))
        };

        if (parent.NodeType != expectedParentType)
        {
            throw new ArgumentException(
                $"A {nodeType} must be created under a {expectedParentType}, not a {parent.NodeType}.",
                nameof(parent));
        }

        return new HierarchyNode
        {
            NodeType = nodeType,
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
