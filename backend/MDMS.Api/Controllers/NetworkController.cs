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
/// Transformer levels, driving the Feeder/DTR tabs of the tree-like Consumer/DTR/Feeder
/// navigation. Every row carries its full Region/Zone/Circle/Division/Sub-Division/Section office
/// chain, resolved by walking up to the owning Substation's <see cref="HierarchyNode.OrgUnitId"/>
/// through the shared <see cref="OrgUnitChainResolver"/>.
/// </summary>
[ApiController]
[Route("api/v1/network")]
public class NetworkController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public NetworkController(IMdmsDbContext db) => _db = db;

    private static (int page, int pageSize) Page(int? page, int? pageSize) => ReportPaging.Normalize(page, pageSize);

    // --------------------------------------------------------------------------------- Feeders

    public record FeederRow(
        Guid Id, string Code, string Name,
        string SubstationCode, string SubstationName,
        string? Region, string? Zone, string? Circle, string? Division, string? SubDivision, string? Section,
        decimal? CapacityKva, string? VoltageLevel, string? Make, DateOnly? CommissionedOn, string? OperationalStatus,
        decimal? Latitude, decimal? Longitude, int DtrCount,
        string? MeterSerial, decimal? MultiplyingFactor, string? ExternalCtRatio, string? ExternalPtRatio,
        decimal? Mect, decimal? Mept, string? FeederMode, string? InstalledBy);

    [HttpGet("feeders")]
    public async Task<IActionResult> ListFeeders(
        [FromQuery] Guid? orgUnitId, [FromQuery] Guid? substationId, [FromQuery] string? search,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);
        var allNodes = await _db.HierarchyNodes.ToListAsync(ct);
        var byId = allNodes.ToDictionary(n => n.Id);
        var resolveOrgUnit = OrgUnitChainResolver.Build(await _db.OrgUnits.ToListAsync(ct));

        var dtrCountByFeeder = allNodes.Where(n => n.NodeType == HierarchyNodeType.DistributionTransformer && n.ParentId.HasValue)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var rows = allNodes.Where(n => n.NodeType == HierarchyNodeType.Feeder)
            .Where(f => substationId is null || f.ParentId == substationId)
            .Select(f =>
            {
                var substation = f.ParentId.HasValue && byId.TryGetValue(f.ParentId.Value, out var s) ? s : null;
                var chain = resolveOrgUnit(substation?.OrgUnitId);
                var row = new FeederRow(
                    f.Id, f.Code, f.Name,
                    substation?.Code ?? "", substation?.Name ?? "",
                    chain.Region?.Name, chain.Zone?.Name, chain.Circle?.Name, chain.Division?.Name, chain.SubDivision?.Name, chain.Section?.Name,
                    f.CapacityKva, f.VoltageLevel, f.Make ?? f.MeterMake, f.CommissionedOn, f.OperationalStatus,
                    f.Latitude, f.Longitude, dtrCountByFeeder.GetValueOrDefault(f.Id, 0),
                    f.MeterSerial, f.MultiplyingFactor, f.ExternalCtRatio, f.ExternalPtRatio, f.Mect, f.Mept, f.FeederMode, f.InstalledBy);
                return (row, chain);
            })
            .ToList();

        if (orgUnitId.HasValue)
            rows = rows.Where(r => r.chain.Contains(orgUnitId.Value)).ToList();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            rows = rows.Where(r => r.row.Code.Contains(s, StringComparison.OrdinalIgnoreCase)
                || r.row.Name.Contains(s, StringComparison.OrdinalIgnoreCase)
                || (r.row.MeterSerial?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }
        var ordered = rows.Select(r => r.row).OrderBy(r => r.Code).ToList();
        var total = ordered.Count;

        if (export == "csv")
        {
            var csv = CsvWriter.Write(
                ["Feeder Code", "Feeder Name", "Substation Code", "Substation Name", "Region", "Zone", "Circle", "Division", "Sub Division", "Section",
                 "Capacity (kVA)", "Voltage Level", "Make", "Commissioned On", "Operational Status", "Latitude", "Longitude", "DTR Count",
                 "MSN", "MF", "External CT Ratio", "External PT Ratio", "MECT", "MEPT", "Mode", "Installed By"],
                ordered, r => [r.Code, r.Name, r.SubstationCode, r.SubstationName, r.Region, r.Zone, r.Circle, r.Division, r.SubDivision, r.Section,
                    r.CapacityKva, r.VoltageLevel, r.Make, r.CommissionedOn?.ToString("yyyy-MM-dd"), r.OperationalStatus, r.Latitude, r.Longitude, r.DtrCount,
                    r.MeterSerial, r.MultiplyingFactor, r.ExternalCtRatio, r.ExternalPtRatio, r.Mect, r.Mept, r.FeederMode, r.InstalledBy]);
            return File(csv, "text/csv", $"MDMS_Feeders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = ordered.Skip((p - 1) * size).Take(size).ToList();
        return Ok(new ListResult<FeederRow>(pageRows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }

    // ------------------------------------------------------------------------------------ DTRs

    public record DtrRow(
        Guid Id, string Code, string Name,
        string FeederCode, string FeederName, string SubstationCode, string SubstationName,
        string? Region, string? Zone, string? Circle, string? Division, string? SubDivision, string? Section,
        decimal? CapacityKva, string? VoltageLevel, string? Make, DateOnly? CommissionedOn, string? OperationalStatus,
        decimal? Latitude, decimal? Longitude, int ConsumerCount,
        string? MeterSerial, decimal? MultiplyingFactor, string? ExternalCtRatio, string? DtrType, string? InstalledBy);

    [HttpGet("dtrs")]
    public async Task<IActionResult> ListDtrs(
        [FromQuery] Guid? orgUnitId, [FromQuery] Guid? feederId, [FromQuery] string? search,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);
        var allNodes = await _db.HierarchyNodes.ToListAsync(ct);
        var byId = allNodes.ToDictionary(n => n.Id);
        var resolveOrgUnit = OrgUnitChainResolver.Build(await _db.OrgUnits.ToListAsync(ct));

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
                var row = new DtrRow(
                    dt.Id, dt.Code, dt.Name,
                    feeder?.Code ?? "", feeder?.Name ?? "", substation?.Code ?? "", substation?.Name ?? "",
                    chain.Region?.Name, chain.Zone?.Name, chain.Circle?.Name, chain.Division?.Name, chain.SubDivision?.Name, chain.Section?.Name,
                    dt.CapacityKva, dt.VoltageLevel, dt.Make ?? dt.MeterMake, dt.CommissionedOn, dt.OperationalStatus,
                    dt.Latitude, dt.Longitude, servicePointCountByDt.GetValueOrDefault(dt.Id, 0),
                    dt.MeterSerial, dt.MultiplyingFactor, dt.ExternalCtRatio, dt.DtrType, dt.InstalledBy);
                return (row, chain);
            })
            .ToList();

        if (orgUnitId.HasValue)
            rows = rows.Where(r => r.chain.Contains(orgUnitId.Value)).ToList();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            rows = rows.Where(r => r.row.Code.Contains(s, StringComparison.OrdinalIgnoreCase)
                || r.row.Name.Contains(s, StringComparison.OrdinalIgnoreCase)
                || (r.row.MeterSerial?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }
        var ordered = rows.Select(r => r.row).OrderBy(r => r.Code).ToList();
        var total = ordered.Count;

        if (export == "csv")
        {
            var csv = CsvWriter.Write(
                ["DTR Code", "DTR Name", "Feeder Code", "Feeder Name", "Substation Code", "Substation Name",
                 "Region", "Zone", "Circle", "Division", "Sub Division", "Section",
                 "Capacity (kVA)", "Voltage Level", "Make", "Commissioned On", "Operational Status", "Latitude", "Longitude", "Consumer Count",
                 "MSN", "MF", "External CT Ratio", "DTR Type", "Installed By"],
                ordered, r => [r.Code, r.Name, r.FeederCode, r.FeederName, r.SubstationCode, r.SubstationName,
                    r.Region, r.Zone, r.Circle, r.Division, r.SubDivision, r.Section,
                    r.CapacityKva, r.VoltageLevel, r.Make, r.CommissionedOn?.ToString("yyyy-MM-dd"), r.OperationalStatus, r.Latitude, r.Longitude, r.ConsumerCount,
                    r.MeterSerial, r.MultiplyingFactor, r.ExternalCtRatio, r.DtrType, r.InstalledBy]);
            return File(csv, "text/csv", $"MDMS_DTRs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = ordered.Skip((p - 1) * size).Take(size).ToList();
        return Ok(new ListResult<DtrRow>(pageRows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }
}
