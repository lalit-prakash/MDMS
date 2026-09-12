namespace MDMS.Domain.Enums;

/// <summary>
/// How a measurement value came to exist. Explicit and never inferred — any downstream consumer
/// must be able to tell a genuine meter-reported value from one this system produced on the
/// meter's behalf.
/// </summary>
public enum MeasurementSource
{
    /// <summary>Reported directly by the meter/HES, unmodified.</summary>
    Received = 1,

    /// <summary>Produced by MDMS's estimation logic because no real reading arrived.</summary>
    Estimated = 2,

    /// <summary>Manually corrected by an operator; the original received value is preserved separately.</summary>
    Edited = 3,

    /// <summary>Derived by calculation from other measurements (e.g. an aggregate), not itself observed.</summary>
    Calculated = 4
}
