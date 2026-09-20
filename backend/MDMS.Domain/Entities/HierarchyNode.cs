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

    /// <summary>
    /// The organizational unit (<see cref="OrgUnit"/>, normally a Section) this node's
    /// administrative office falls under — only ever set on a Substation, the electrical
    /// hierarchy's top level, since that's the only level with an independent office assignment;
    /// a Feeder/DT inherits its office through its Substation ancestor. Lets a Region/Zone/
    /// Circle/Division/Sub-Division filter resolve to a set of Substations (and everything
    /// beneath them) without duplicating the org chain on every node.
    /// </summary>
    public Guid? OrgUnitId { get; private set; }

    /// <summary>
    /// Master-data fields shared by Feeder and DT master lists per the reference Feeder/DTR
    /// Excel sheets — all nullable and set only when the caller actually supplies them via
    /// <see cref="SetMasterData"/>, never fabricated.
    /// </summary>
    public decimal? CapacityKva { get; private set; }
    public string? VoltageLevel { get; private set; }
    public string? Make { get; private set; }
    public DateOnly? CommissionedOn { get; private set; }
    public string? OperationalStatus { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }

    /// <summary>
    /// Metering-asset fields from the Feeder/DTR master sheets (the feeder/DT's own meter and its
    /// CT/PT setup) � all nullable, set only via <see cref="SetAssetData"/>.
    /// </summary>
    public string? MeterSerial { get; private set; }
    public string? MeterMake { get; private set; }
    public decimal? MultiplyingFactor { get; private set; }
    public string? ExternalCtRatio { get; private set; }
    public string? ExternalPtRatio { get; private set; }
    public decimal? Mect { get; private set; }
    public decimal? Mept { get; private set; }
    public string? FeederMode { get; private set; }
    public string? DtrType { get; private set; }
    public string? InstalledBy { get; private set; }
    public int? Satno { get; private set; }
    public DateTime? MdmAssetTimestampUtc { get; private set; }

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

    /// <summary>Links this node's administrative office. Only meaningful on a Substation — see
    /// <see cref="OrgUnitId"/>.</summary>
    public void AssignOrgUnit(OrgUnit orgUnit)
    {
        ArgumentNullException.ThrowIfNull(orgUnit);
        if (NodeType != HierarchyNodeType.Substation)
            throw new InvalidOperationException($"Only a Substation carries an OrgUnit assignment (was {NodeType}).");
        OrgUnitId = orgUnit.Id;
    }

    /// <summary>Sets the optional Feeder/DT master-data fields. Safe to call on any node
    /// regardless of type.</summary>
    public void SetMasterData(
        decimal? capacityKva, string? voltageLevel, string? make,
        DateOnly? commissionedOn, string? operationalStatus, decimal? latitude, decimal? longitude)
    {
        CapacityKva = capacityKva;
        VoltageLevel = voltageLevel;
        Make = make;
        CommissionedOn = commissionedOn;
        OperationalStatus = operationalStatus;
        Latitude = latitude;
        Longitude = longitude;
    }

    public void SetAssetData(
        string? meterSerial, string? meterMake, decimal? multiplyingFactor, string? externalCtRatio, string? externalPtRatio,
        decimal? mect, decimal? mept, string? feederMode, string? dtrType, string? installedBy, int? satno, DateTime? mdmAssetTimestampUtc)
    {
        MeterSerial = meterSerial; MeterMake = meterMake; MultiplyingFactor = multiplyingFactor;
        ExternalCtRatio = externalCtRatio; ExternalPtRatio = externalPtRatio; Mect = mect; Mept = mept;
        FeederMode = feederMode; DtrType = dtrType; InstalledBy = installedBy; Satno = satno; MdmAssetTimestampUtc = mdmAssetTimestampUtc;
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
