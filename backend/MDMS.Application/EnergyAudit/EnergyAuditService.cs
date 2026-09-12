using MDMS.Application.Common;
using MDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Application.EnergyAudit;

/// <summary>
/// Compares energy entering a network level (a Substation, Feeder, or Distribution Transformer
/// node) against energy accounted for downstream, per the project's energy-audit requirement.
/// Deliberately does not compute a single "loss %" collapsing real loss and data-coverage gaps
/// into one number — see <see cref="EnergyBalanceResult"/> for why.
/// </summary>
public class EnergyAuditService
{
    private readonly IMdmsDbContext _db;

    public EnergyAuditService(IMdmsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Every Distribution Transformer node at or beneath <paramref name="hierarchyNodeId"/> — the
    /// node itself if it already is a DT, otherwise every DT descendant reachable through the
    /// Substation → Feeder → DT tree. A consumer only ever attaches at the DT level
    /// (<see cref="ServicePoint.DistributionTransformerNodeId"/>), so this is the set that
    /// determines which consumers roll up under any given node.
    /// </summary>
    public async Task<List<Guid>> GetDescendantDistributionTransformerIdsAsync(
        Guid hierarchyNodeId, CancellationToken cancellationToken = default)
    {
        var allNodes = await _db.HierarchyNodes.ToListAsync(cancellationToken);
        var nodesById = allNodes.ToDictionary(n => n.Id);

        if (!nodesById.TryGetValue(hierarchyNodeId, out var root))
            return new List<Guid>();

        if (root.NodeType == Domain.Enums.HierarchyNodeType.DistributionTransformer)
            return new List<Guid> { root.Id };

        var childrenByParent = allNodes
            .Where(n => n.ParentId is not null)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(hierarchyNodeId);

        while (stack.Count > 0)
        {
            var currentId = stack.Pop();
            if (!childrenByParent.TryGetValue(currentId, out var children))
                continue;

            foreach (var child in children)
            {
                if (child.NodeType == Domain.Enums.HierarchyNodeType.DistributionTransformer)
                    result.Add(child.Id);
                else
                    stack.Push(child.Id);
            }
        }

        return result;
    }

    /// <summary>
    /// Computes the transparent energy balance for <paramref name="hierarchyNodeId"/> on
    /// <paramref name="date"/>: supply-side energy (if a reading exists for that exact node — not
    /// aggregated up from children, since that's a separate, larger assumption) against the sum of
    /// every downstream consumer's Daily Load Profile for that day, plus a data-completeness
    /// figure (how many of the linked service points actually reported a profile at all).
    /// </summary>
    public async Task<EnergyBalanceResult> ComputeDailyBalanceAsync(
        Guid hierarchyNodeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var inputReading = await _db.NetworkEnergyReadings
            .FirstOrDefaultAsync(r => r.HierarchyNodeId == hierarchyNodeId && r.Date == date, cancellationToken);

        var dtIds = await GetDescendantDistributionTransformerIdsAsync(hierarchyNodeId, cancellationToken);

        var linkedServicePointIds = await _db.ServicePoints
            .Where(sp => sp.DistributionTransformerNodeId != null && dtIds.Contains(sp.DistributionTransformerNodeId!.Value))
            .Select(sp => sp.Id)
            .ToListAsync(cancellationToken);

        var profilesForDate = await _db.DailyLoadProfiles
            .Where(p => linkedServicePointIds.Contains(p.ServicePointId) && p.ProfileDate == date)
            .ToListAsync(cancellationToken);

        var accountedEnergy = profilesForDate.Sum(p => p.ConsumptionKwh);
        var servicePointsWithData = profilesForDate.Select(p => p.ServicePointId).Distinct().Count();

        decimal? discrepancy = inputReading is null ? null : inputReading.EnergyKwh - accountedEnergy;
        decimal? discrepancyPercent = inputReading is { EnergyKwh: > 0 } && discrepancy is not null
            ? discrepancy.Value / inputReading.EnergyKwh * 100m
            : null;
        decimal? completeness = linkedServicePointIds.Count == 0
            ? null
            : (decimal)servicePointsWithData / linkedServicePointIds.Count * 100m;

        return new EnergyBalanceResult(
            hierarchyNodeId, date, inputReading?.EnergyKwh, accountedEnergy, discrepancy, discrepancyPercent,
            linkedServicePointIds.Count, servicePointsWithData, completeness);
    }
}
