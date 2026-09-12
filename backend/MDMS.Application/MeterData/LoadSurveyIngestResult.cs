using MDMS.Domain.Enums;

namespace MDMS.Application.MeterData;

/// <summary>Per-item outcome of a batch LS ingest — a bad item never fails the rest of the batch.</summary>
public record LoadSurveyIngestResult(
    Guid MeterId,
    DateTime IntervalStartUtc,
    DateTime IntervalEndUtc,
    MeasurementQuality Quality,
    Guid IntervalId);
