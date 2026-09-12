using MDMS.Domain.Enums;

namespace MDMS.Application.MeterData;

/// <summary>Outcome of ingesting one Daily Load Profile.</summary>
public record DailyLoadProfileIngestResult(
    Guid ProfileId,
    Guid ServicePointId,
    Guid MeterId,
    DateOnly ProfileDate,
    decimal ConsumptionKwh,
    MeasurementSource Source,
    bool ReplacedProvisional);
