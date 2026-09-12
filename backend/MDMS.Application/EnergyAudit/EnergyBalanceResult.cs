namespace MDMS.Application.EnergyAudit;

/// <summary>
/// The transparent result of one energy-balance calculation for one hierarchy node/day. Per the
/// project's own principle, this deliberately does not collapse everything into a single "loss %"
/// — it exposes the inputs (supply-side energy, downstream-accounted energy, data completeness)
/// so a reader can judge how much of any discrepancy is a genuine loss versus a data-coverage gap,
/// rather than a fabricated real-loss/coverage-loss split with no justified formula behind it.
/// </summary>
public record EnergyBalanceResult(
    Guid HierarchyNodeId,
    DateOnly Date,
    decimal? InputEnergyKwh,
    decimal AccountedEnergyKwh,
    decimal? DiscrepancyKwh,
    decimal? DiscrepancyPercent,
    int LinkedServicePointCount,
    int ServicePointsWithDataCount,
    decimal? DataCompletenessPercent);
