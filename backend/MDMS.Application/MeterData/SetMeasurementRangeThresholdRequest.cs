using MDMS.Domain.Enums;

namespace MDMS.Application.MeterData;

/// <summary>
/// Configures a plausibility range for a given measurement product's consumption.
/// <see cref="MeterId"/> null sets the global default for <see cref="MeasurementType"/>; a value
/// sets a meter-specific override.
/// </summary>
public record SetMeasurementRangeThresholdRequest(
    MeasurementRangeType MeasurementType,
    Guid? MeterId,
    decimal MinConsumptionKwh,
    decimal MaxConsumptionKwh);
