namespace MDMS.Application.MeterData;

/// <summary>One incoming Daily Load Profile, as submitted by an ingestion caller (HES/simulator/test harness).</summary>
public record DailyLoadProfileIngestRequest(
    Guid ServicePointId,
    Guid MeterId,
    DateOnly ProfileDate,
    decimal ConsumptionKwh);
