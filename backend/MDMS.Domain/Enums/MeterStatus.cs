namespace MDMS.Domain.Enums;

/// <summary>Lifecycle status of a physical meter asset within MDMS.</summary>
public enum MeterStatus
{
    /// <summary>In stock, not yet installed at a service point.</summary>
    InStock = 1,

    /// <summary>Installed and actively reporting/expected to report data.</summary>
    Installed = 2,

    /// <summary>Physically removed (replacement, decommission) but retained for history.</summary>
    Removed = 3,

    /// <summary>Retired from the fleet entirely (scrapped, returned to vendor).</summary>
    Retired = 4
}
