namespace MDMS.Domain.Enums;

/// <summary>
/// Which kind of VEE rule a <see cref="Entities.VeeRuleDefinition"/> row is. New rule types
/// (continuity, missing-interval estimation, ...) get their own subclass and value here rather
/// than overloading an existing one.
/// </summary>
public enum VeeRuleType
{
    /// <summary>A <see cref="Entities.MeasurementRangeThreshold"/> plausibility range.</summary>
    OutOfRange = 1
}
