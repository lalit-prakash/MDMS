namespace MDMS.Domain.Enums;

/// <summary>
/// Electrical phase configuration of a meter. Mirrors prepaid_engine's <c>MeterPhase</c>
/// naming so a meter record maps 1:1 across the two systems' shared vocabulary.
/// </summary>
public enum MeterPhase
{
    Single = 1,
    Three = 3
}
