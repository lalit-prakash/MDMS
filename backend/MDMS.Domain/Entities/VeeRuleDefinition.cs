using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// Base type for a stored, configurable VEE (Validation/Estimation/Editing) rule — data, not a
/// hard-coded constant in a validation service, per MDMS's "configuration over hard-coded rules"
/// principle. <see cref="MeasurementRangeThreshold"/> is the first concrete rule type
/// (out-of-range plausibility); future rule types (continuity, missing-interval estimation) get
/// their own subclass and <see cref="VeeRuleType"/> value rather than overloading this one.
/// </summary>
/// <remarks>
/// Stored as one table (table-per-hierarchy) discriminated by <see cref="RuleType"/>, so a config
/// screen can list every rule of every type in one query while each rule type keeps its own
/// strongly-typed parameters as its own columns — no generic JSON blob to parse and no separate
/// table per rule type to join across.
/// </remarks>
public abstract class VeeRuleDefinition : Entity
{
    public VeeRuleType RuleType { get; private set; }

    /// <summary><c>null</c> means this rule is the global default for its type; a value scopes it to one meter.</summary>
    public Guid? MeterId { get; private set; }

    /// <summary>
    /// Whether this rule is currently enforced. Not yet consulted by every rule type's evaluation
    /// logic — <see cref="MeasurementRangeThreshold"/>'s own effective-threshold lookup already
    /// filters on it; a future rule type must do the same rather than always treating itself as active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    protected VeeRuleDefinition() { }

    protected VeeRuleDefinition(VeeRuleType ruleType, Guid? meterId)
    {
        RuleType = ruleType;
        MeterId = meterId;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
