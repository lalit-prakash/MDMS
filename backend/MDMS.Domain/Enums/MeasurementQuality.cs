namespace MDMS.Domain.Enums;

/// <summary>
/// VEE (Validation/Estimation/Editing) outcome for a measurement — VEE is MDMS's own
/// responsibility, not any downstream billing system's.
/// </summary>
public enum MeasurementQuality
{
    /// <summary>Passed all configured validation checks.</summary>
    Valid = 1,

    /// <summary>Cumulative value decreased relative to the prior reading for the same meter.</summary>
    NegativeConsumption = 2,

    /// <summary>Value falls outside a configured plausibility range.</summary>
    OutOfRange = 3,

    /// <summary>Reading is missing/did not arrive; a placeholder/estimate may be substituted.</summary>
    Missing = 4,

    /// <summary>Flagged by a validation rule but not yet resolved by an operator.</summary>
    Suspect = 5
}
