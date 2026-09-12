namespace MDMS.Application.MeterData;

/// <summary>One incoming 30-minute LS block, as submitted by an ingestion caller (HES/simulator/test harness).</summary>
public record LoadSurveyIngestRequest(
    Guid MeterId,
    DateTime IntervalStartUtc,
    DateTime IntervalEndUtc,
    decimal CumulativeReading);
