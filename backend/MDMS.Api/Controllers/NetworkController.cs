using MDMS.Api.Reporting;
using MDMS.Application.Common;
using MDMS.Application.Reporting;
using MDMS.Domain.Entities;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Read-only master-data listings for the electrical hierarchy's Feeder and Distribution
/// Transformer levels — the Feeder/DTR-scoped counterpart to <see cref="MetersController"/> and
/// <see cref="CustomersController"/>, driving the Feeder/DTR tabs of the tree-like
/// Consumer/DTR/Feeder navigation. Every row carries its full Region/Zone/Circle/Division/
/// Sub-Division/Section office chain, resolved by walking up to the owning Substation's
/// <see cref="HierarchyNode.OrgUnitId"/> — the same real <see cref="OrgUnit"/> tree
/// <see cref="ConfigController"/> manages, not a fabricated one.
/// </summary>
[ApiController]
[Route("api/v1/network")]
public class NetworkController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public NetworkController(IMdmsDbContext db) => _db = db;

    private static (int page, int pageSize) Page(int? page, int? pageSize) => ReportPaging.Normalize(page, pageSize);

    public record OrgUnitChain(string? Zone, string? Circle, string? Division, string? SubDivision, string? Section);

    /// <summary>Loads every OrgUnit once and returns a resolver from a Substation's OrgUnitId to
    /// its full Zone→Section ancestor chain — cheap at this project's data scale, and avoids one
    /// query per row.</summary>
    private async Task<Func<Guid?, OrgUnitChain>> BuildOrgUnitResolverAsync(CancellationToken ct)
    {
        var orgUnits = await _db.OrgUnits.ToListAsync(ct);
        var byId = orgUnits.ToDictionary(u => u.Id);

        return orgUnitId =>
        {
            string? zone = null, circle = null, division = null, subDivision = null, section = null;
            var current = orgUnitId.HasValue && byId.TryGetValue(orgUnitId.Value, out var start) ? start : null;
            while (current is not null)
            {
                switch (current.UnitType)
                {
                    case OrgUnitType.Zone: zone = current.Name; break;
                    case OrgUnitType.Circle: circle = current.Name; break;
                    case OrgUnitType.Division: division = current.Name; break;
                    case OrgUnitType.SubDivision: subDivision = current.Name; break;
                    case OrgUnitType.Section: section = current.Name; break;
                }
                current = current.ParentId.HasValue && byId.TryGetValue(current.ParentId.Value, out var parent) ? parent : null;
            }
            return new OrgUnitChain(zone, circle, division, subDivision, section);
        };
    }

    // --------------------------------------------------------------------------------- Feeders

    public record FeederRow(
        Guid Id, string Code, string Name,
        string SubstationCode, string SubstationName,
        string? Zone, string? Circle, string? Division, string? SubDivision, string? Section,
        decimal? CapacityKva, string? VoltageLevel, string? Make, DateOnly? CommissionedOn, string? OperationalStatus,
        decimal? Latitude, decimal? Longitude, int DtrCount);

    [HttpGet("feeders")]
    public async Task<IActionResult> ListFeeders(
        [FromQuery] Guid? orgUnitId, [FromQuery] Guid? substationId, [FromQuery] string? search,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);
        var allNodes = await _db.HierarchyNodes.ToListAsync(ct);
        var byId = allNodes.ToDictionary(n => n.Id);
        var resolveOrgUnit = await BuildOrgUnitResolverAsync(ct);

        var dtrCountByFeeder = allNodes.Where(n => n.NodeType == HierarchyNodeType.DistributionTransformer && n.ParentId.HasValue)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var rows = allNodes.Where(n => n.NodeType == HierarchyNodeType.Feeder)
            .Where(f => substationId is null || f.ParentId == substationId)
            .Select(f =>
            {
                var substation = f.ParentId.HasValue && byId.TryGetValue(f.ParentId.Value, out var s) ? s : null;
                var chain = resolveOrgUnit(substation?.OrgUnitId);
                return new FeederRow(
                    f.Id, f.Code, f.Name,
                    substation?.Code ?? "", substation?.Name ?? "",
                    chain.Zone, chain.Circle, chain.Division, chain.SubDivision, chain.Section,
                    f.CapacityKva, f.VoltageLevel, f.Make, f.CommissionedOn, f.OperationalStatus,
                    f.Latitude, f.Longitude, dtrCountByFeeder.GetValueOrDefault(f.Id, 0));
            })
            .ToList();

        if (orgUnitId.HasValue)
        {
            var targetChain = resolveOrgUnit(orgUnitId);
            rows = rows.Where(r =>
                r.Zone == targetChain.Zone || r.Circle == targetChain.Circle || r.Division == targetChain.Division ||
                r.SubDivision == targetChain.SubDivision || r.Section == targetChain.Section).ToList();
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            rows = rows.Where(r => r.Code.Contains(s, StringComparison.OrdinalIgnoreCase) || r.Name.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        rows = rows.OrderBy(r => r.Code).ToList();

        var total = rows.Count;

        if (export == "csv")
        {
            var csv = CsvWriter.Write(
                ["Feeder Code", "Feeder Name", "Substation Code", "Substation Name", "Zone", "Circle", "Division", "Sub Division", "Section",
                 "Capacity (kVA)", "Voltage Level", "Make", "Commissioned On", "Operational Status", "Latitude", "Longitude", "DTR Count"],
                rows, r => [r.Code, r.Name, r.SubstationCode, r.SubstationName, r.Zone, r.Circle, r.Division, r.SubDivision, r.Section,
                    r.CapacityKva, r.VoltageLevel, r.Make, r.CommissionedOn?.ToString("yyyy-MM-dd"), r.OperationalStatus, r.Latitude, r.Longitude, r.DtrCount]);
            return File(csv, "text/csv", $"MDMS_Feeders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = rows.Skip((p - 1) * size).Take(size).ToList();
        return Ok(new ListResult<FeederRow>(pageRows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }

    // ------------------------------------------------------------------------------------ DTRs

    public record DtrRow(
        Guid Id, string Code, string Name,
        string FeederCode, string FeederName, string SubstationCode, string SubstationName,
        string? Zone, string? Circle, string? Division, string? SubDivision, string? Section,
        decimal? CapacityKva, string? VoltageLevel, string? Make, DateOnly? CommissionedOn, string? OperationalStatus,
        decimal? Latitude, decimal? Longitude, int ConsumerCount);

    [HttpGet("dtrs")]
    public async Task<IActionResult> ListDtrs(
        [FromQuery] Guid? orgUnitId, [FromQuery] Guid? feederId, [FromQuery] string? search,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);
        var allNodes = await _db.HierarchyNodes.ToListAsync(ct);
        var byId = allNodes.ToDictionary(n => n.Id);
        var resolveOrgUnit = await BuildOrgUnitResolverAsync(ct);

        var servicePointCountByDt = (await _db.ServicePoints.Where(sp => sp.DistributionTransformerNodeId != null).ToListAsync(ct))
            .GroupBy(sp => sp.DistributionTransformerNodeId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var rows = allNodes.Where(n => n.NodeType == HierarchyNodeType.DistributionTransformer)
            .Where(n => feederId is null || n.ParentId == feederId)
            .Select(dt =>
            {
                var feeder = dt.ParentId.HasValue && byId.TryGetValue(dt.ParentId.Value, out var f) ? f : null;
                var substation = feeder?.ParentId.HasValue == true && byId.TryGetValue(feeder.ParentId!.Value, out var s) ? s : null;
                var chain = resolveOrgUnit(substation?.OrgUnitId);
                return new DtrRow(
                    dt.Id, dt.Code, dt.Name,
                    feeder?.Code ?? "", feeder?.Name ?? "", substation?.Code ?? "", substation?.Name ?? "",
                    chain.Zone, chain.Circle, chain.Division, chain.SubDivision, chain.Section,
                    dt.CapacityKva, dt.VoltageLevel, dt.Make, dt.CommissionedOn, dt.OperationalStatus,
                    dt.Latitude, dt.Longitude, servicePointCountByDt.GetValueOrDefault(dt.Id, 0));
            })
            .ToList();

        if (orgUnitId.HasValue)
        {
            var targetChain = resolveOrgUnit(orgUnitId);
            rows = rows.Where(r =>
                r.Zone == targetChain.Zone || r.Circle == targetChain.Circle || r.Division == targetChain.Division ||
                r.SubDivision == targetChain.SubDivision || r.Section == targetChain.Section).ToList();
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            rows = rows.Where(r => r.Code.Contains(s, StringComparison.OrdinalIgnoreCase) || r.Name.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        rows = rows.OrderBy(r => r.Code).ToList();

        var total = rows.Count;

        if (export == "csv")
        {
            var csv = CsvWriter.Write(
                ["DTR Code", "DTR Name", "Feeder Code", "Feeder Name", "Substation Code", "Substation Name",
                 "Zone", "Circle", "Division", "Sub Division", "Section",
                 "Capacity (kVA)", "Voltage Level", "Make", "Commissioned On", "Operational Status", "Latitude", "Longitude", "Consumer Count"],
                rows, r => [r.Code, r.Name, r.FeederCode, r.FeederName, r.SubstationCode, r.SubstationName,
                    r.Zone, r.Circle, r.Division, r.SubDivision, r.Section,
                    r.CapacityKva, r.VoltageLevel, r.Make, r.CommissionedOn?.ToString("yyyy-MM-dd"), r.OperationalStatus, r.Latitude, r.Longitude, r.ConsumerCount]);
            return File(csv, "text/csv", $"MDMS_DTRs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = rows.Skip((p - 1) * size).Take(size).ToList();
        return Ok(new ListResult<DtrRow>(pageRows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }
}
