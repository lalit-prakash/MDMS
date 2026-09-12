using MDMS.Domain.Common;

namespace MDMS.Domain.Entities;

/// <summary>
/// A configurable plausibility range for Load Survey interval consumption, used by out-of-range
/// VEE checks. Per MDMS's "configuration over hard-coded rules" principle, thresholds are data,
/// never a hard-coded constant in the validation service.
/// </summary>
/// <remarks>
/// <see cref="MeterId"/> null means this is the global default, used for any meter without its
/// own meter-specific threshold. At most one global threshold and one threshold per meter is
/// meaningful; enforcing that uniqueness is left to the caller/service layer rather than a DB
/// constraint, since "the newest one wins" is a simpler, sufficient rule for this phase.
/// </remarks>
public class MeasurementRangeThreshold : Entity
{
    public Guid? MeterId { get; private set; }
    public decimal MinConsumptionKwh { get; private set; }
    public decimal MaxConsumptionKwh { get; private set; }

    private MeasurementRangeThreshold() { }

    public MeasurementRangeThreshold(Guid? meterId, decimal minConsumptionKwh, decimal maxConsumptionKwh)
    {
        if (minConsumptionKwh < 0)
            throw new ArgumentOutOfRangeException(nameof(minConsumptionKwh), "Minimum consumption cannot be negative.");
        if (maxConsumptionKwh <= minConsumptionKwh)
            throw new ArgumentException("Maximum consumption must be greater than the minimum.", nameof(maxConsumptionKwh));

        MeterId = meterId;
        MinConsumptionKwh = minConsumptionKwh;
        MaxConsumptionKwh = maxConsumptionKwh;
    }

    public bool IsWithinRange(decimal consumptionKwh)
        => consumptionKwh >= MinConsumptionKwh && consumptionKwh <= MaxConsumptionKwh;
}
