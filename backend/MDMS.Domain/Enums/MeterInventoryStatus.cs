namespace MDMS.Domain.Enums;

/// <summary>
/// The WFM/inventory workflow status of a physical meter — deliberately separate from
/// <see cref="MeterStatus"/> (the meter's own simple master-data lifecycle). This tracks *where
/// the meter is in the allocation/installation process*, which involves actors (store manager,
/// contractor, installer) and a state machine <see cref="MeterStatus"/> was never meant to carry.
/// Mirrors the spec's stated inventory flow exactly, including the replacement sub-flow.
/// </summary>
public enum MeterInventoryStatus
{
    Received = 1,
    InStore = 2,
    AssignedToContractor = 3,
    AssignedToInstaller = 4,
    Installed = 5,
    Commissioned = 6,
    Active = 7,

    // Replacement sub-flow (from Active):
    ReplacementRequested = 8,
    Removed = 9
}
