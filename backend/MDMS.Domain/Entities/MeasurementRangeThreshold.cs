using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// A configurable plausibility range for a given measurement product's consumption (Load Survey
/// interval or Daily Load Profile), used by out-of-range VEE checks. Per MDMS's "configuration
/// over hard-coded rules" principle, thresholds are data, never a hard-coded constant in the
/// validation service — and a 30-minute LS interval's plausible range is nowhere near a full
/// day's DLP range, so the two are always configured (and looked up) separately via
/// <see cref="MeasurementType"/>. The first concrete <see cref="VeeRuleDefinition"/> — future
/// rule types (continuity, missing-interval estimation) are new subclasses alongside this one.
/// </summary>
/// <remarks>
/// <see cref="VeeRuleDefinition.MeterId"/> null means this is the global default for that
/// <see cref="MeasurementType"/>, used for any meter without its own meter-specific threshold of
/// that type. At most one global threshold and one meter-specific threshold per (type, meter) is
/// meaningful; enforcing that uniqueness is left to the caller/service layer rather than a DB
/// constraint, since "the newest one wins" is a simpler, sufficient rule for this phase.
/// </remarks>
public class MeasurementRangeThreshold : VeeRuleDefinition
{
    public MeasurementRangeType MeasurementType { get; private set; }
    public decimal MinConsumptionKwh { get; private set; }
    public decimal MaxConsumptionKwh { get; private set; }

    private MeasurementRangeThreshold() { }

    public MeasurementRangeThreshold(
        MeasurementRangeType measurementType, Guid? meterId, decimal minConsumptionKwh, decimal maxConsumptionKwh)
        : base(VeeRuleType.OutOfRange, meterId)
    {
        if (minConsumptionKwh < 0)
            throw new ArgumentOutOfRangeException(nameof(minConsumptionKwh), "Minimum consumption cannot be negative.");
        if (maxConsumptionKwh <= minConsumptionKwh)
            throw new ArgumentException("Maximum consumption must be greater than the minimum.", nameof(maxConsumptionKwh));

        MeasurementType = measurementType;
        MinConsumptionKwh = minConsumptionKwh;
        MaxConsumptionKwh = maxConsumptionKwh;
    }

    public bool IsWithinRange(decimal consumptionKwh)
        => consumptionKwh >= MinConsumptionKwh && consumptionKwh <= MaxConsumptionKwh;
}
