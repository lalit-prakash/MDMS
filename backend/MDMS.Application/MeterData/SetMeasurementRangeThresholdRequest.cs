namespace MDMS.Application.MeterData;

/// <summary>
/// Configures a plausibility range for Load Survey consumption. <see cref="MeterId"/> null sets
/// the global default; a value sets a meter-specific override.
/// </summary>
public record SetMeasurementRangeThresholdRequest(
    Guid? MeterId,
    decimal MinConsumptionKwh,
    decimal MaxConsumptionKwh);
