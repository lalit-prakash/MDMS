namespace MDMS.Domain.Enums;

/// <summary>The spec's stated categories of revenue-protection risk signal.</summary>
public enum RiskSignalType
{
    TamperEvent = 1,
    RepeatedCoverOpen = 2,
    ConsumptionDeviation = 3,
    DtrAnomaly = 4,
    CommunicationManipulation = 5,
    UnbilledMappingAnomaly = 6
}
