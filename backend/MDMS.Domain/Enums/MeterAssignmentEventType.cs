namespace MDMS.Domain.Enums;

/// <summary>Why a meter assignment record was created.</summary>
public enum MeterAssignmentEventType
{
    InitialInstallation = 1,
    Replacement = 2,
    Removal = 3
}
