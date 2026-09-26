using MDMS.Api.Reporting;
using MDMS.Application.Common;
using MDMS.Application.Reporting;
using MDMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Read-only view over Customer + ServicePoint master data — no consumer-facing UI existed for
/// this before now, even though the domain entities have always been there (see Customer.cs).
/// Deliberately thin: MDMS owns this as master data, tariff/billing facts belong to downstream
/// billing systems per Customer's own doc comment, so this never grows those fields.
/// </summary>
[ApiController]
[Route("api/v1/customers")]
public class CustomersController : ControllerBase
{
    private readonly IMdmsDbContext _db;

    public CustomersController(IMdmsDbContext db) => _db = db;

    public record ServicePointSummary(Guid Id, string Address, Guid? DistributionTransformerNodeId);
    public record CustomerResponse(Guid Id, string AccountNumber, string Name, IReadOnlyList<ServicePointSummary> ServicePoints);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, CancellationToken ct)
    {
        var customers = await _db.Customers.OrderBy(c => c.AccountNumber).ToListAsync(ct);
        var servicePoints = await _db.ServicePoints.ToListAsync(ct);

        var results = customers
            .Select(c => new CustomerResponse(
                c.Id, c.AccountNumber, c.Name,
                servicePoints.Where(sp => sp.CustomerId == c.Id)
                    .Select(sp => new ServicePointSummary(sp.Id, sp.Address, sp.DistributionTransformerNodeId))
                    .ToList()))
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            results = results.Where(c => c.AccountNumber.Contains(s, StringComparison.OrdinalIgnoreCase) || c.Name.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        return Ok(results.ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null) return NotFound();

        var servicePoints = await _db.ServicePoints.Where(sp => sp.CustomerId == id).ToListAsync(ct);
        return Ok(new CustomerResponse(
            customer.Id, customer.AccountNumber, customer.Name,
            servicePoints.Select(sp => new ServicePointSummary(sp.Id, sp.Address, sp.DistributionTransformerNodeId)).ToList()));
    }

    public record CreateCustomerRequest(string AccountNumber, string Name);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        Customer customer;
        try
        {
            customer = new Customer(request.AccountNumber, request.Name);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, new CustomerResponse(customer.Id, customer.AccountNumber, customer.Name, []));
    }

    public record SetCustomerMasterDataRequest(
        string? RrNumber, string? MobileNumber, string? ConnectionStatus, DateOnly? ServiceDate,
        decimal? SanctionedLoadKw, decimal? ContractDemandKva, decimal? ConnectedLoadKw,
        string? LoadType, string? TariffCategoryCode, string? CommunicationType, string? PaymentMode,
        bool? IsNetMeter, int? BillDay, string? BillCycle, decimal? Latitude, decimal? Longitude);

    /// <summary>Sets the remaining Consumer master-info fields (RR Number, tariff/load facts,
    /// communication/payment mode, etc.) per the reference Consumer master-info sheet.</summary>
    [HttpPost("{id:guid}/master-data")]
    public async Task<IActionResult> SetMasterData(Guid id, [FromBody] SetCustomerMasterDataRequest request, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null) return NotFound();

        customer.SetMasterData(
            request.RrNumber, request.MobileNumber, request.ConnectionStatus, request.ServiceDate,
            request.SanctionedLoadKw, request.ContractDemandKva, request.ConnectedLoadKw,
            request.LoadType, request.TariffCategoryCode, request.CommunicationType, request.PaymentMode,
            request.IsNetMeter, request.BillDay, request.BillCycle, request.Latitude, request.Longitude);

        await _db.SaveChangesAsync(ct);
        return Ok(customer);
    }

    public record SetCustomerMeterAssetRequest(
        string? MeterMake, string? MeterPhase, decimal? MultiplyingFactor, bool? IsMrRequiredDone,
        int? Satno, DateTime? MdmAssetTimestampUtc, DateOnly? MeterReplacementDate);

    [HttpPost("{id:guid}/meter-asset-data")]
    public async Task<IActionResult> SetMeterAssetData(Guid id, [FromBody] SetCustomerMeterAssetRequest r, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null) return NotFound();
        customer.SetMeterAssetData(r.MeterMake, r.MeterPhase, r.MultiplyingFactor, r.IsMrRequiredDone, r.Satno, r.MdmAssetTimestampUtc, r.MeterReplacementDate);
        await _db.SaveChangesAsync(ct);
        return Ok(customer);
    }

    private static (int page, int pageSize) Page(int? page, int? pageSize) => ReportPaging.Normalize(page, pageSize);

    public record ConsumerMasterRow(
        Guid Id, string AccountNumber, string Name, string? RrNumber, string? MeterNumber,
        string? DtrCode, string? FeederCode, string? SubstationCode,
        string? Region, string? Zone, string? Circle, string? Division, string? SubDivision, string? Section,
        string Address, string? MobileNumber, string? ConnectionStatus, DateOnly? ServiceDate,
        decimal? SanctionedLoadKw, decimal? ContractDemandKva, decimal? ConnectedLoadKw,
        string? LoadType, string? TariffCategoryCode, string? CommunicationType, string? PaymentMode,
        bool? IsNetMeter, int? BillDay, string? BillCycle, decimal? Latitude, decimal? Longitude,
        string? MeterMake, string? MeterPhase, decimal? MultiplyingFactor, bool? IsMrRequiredDone, int? Satno,
        DateTime? MdmAssetTimestampUtc, DateOnly? MeterReplacementDate);

    /// <summary>
    /// The full Consumer master-data listing driving the tree-like Consumer tab: every consumer
    /// with its service point address, DT/Feeder/Substation and Zone→Section office chain (the
    /// same real hierarchy <see cref="NetworkController"/> resolves for Feeders/DTRs), and its
    /// currently-assigned meter's serial number — paginated, server-side filtered, CSV-exportable,
    /// matching the pattern every other master/report list in this project follows.
    /// </summary>
    [HttpGet("master")]
    public async Task<IActionResult> ListMasterData(
        [FromQuery] Guid? orgUnitId, [FromQuery] Guid? dtNodeId, [FromQuery] Guid? feederId, [FromQuery] string? search,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? export, CancellationToken ct)
    {
        var (p, size) = Page(page, pageSize);

        var customers = await _db.Customers.ToListAsync(ct);
        var servicePoints = await _db.ServicePoints.ToListAsync(ct);
        var hierarchyNodes = await _db.HierarchyNodes.ToListAsync(ct);
        var nodesById = hierarchyNodes.ToDictionary(n => n.Id);
        var resolveChain = OrgUnitChainResolver.Build(await _db.OrgUnits.ToListAsync(ct));
        var assignments = await _db.MeterAssignments.Where(a => a.EffectiveToUtc == null).ToListAsync(ct);
        var meters = await _db.Meters.ToListAsync(ct);
        var metersById = meters.ToDictionary(m => m.Id);

        var rows = new List<ConsumerMasterRow>();
        var chains = new Dictionary<Guid, OrgUnitChain>();
        foreach (var c in customers)
        {
            var sp = servicePoints.FirstOrDefault(x => x.CustomerId == c.Id);
            var dt = sp?.DistributionTransformerNodeId.HasValue == true && nodesById.TryGetValue(sp.DistributionTransformerNodeId.Value, out var dtNode) ? dtNode : null;
            var feeder = dt?.ParentId.HasValue == true && nodesById.TryGetValue(dt.ParentId.Value, out var f) ? f : null;
            var substation = feeder?.ParentId.HasValue == true && nodesById.TryGetValue(feeder.ParentId.Value, out var s) ? s : null;

            if (dtNodeId.HasValue && dt?.Id != dtNodeId) continue;
            if (feederId.HasValue && feeder?.Id != feederId) continue;

            var chain = resolveChain(substation?.OrgUnitId);

            var meterId = sp is not null ? assignments.FirstOrDefault(a => a.ServicePointId == sp.Id)?.MeterId : null;
            var meterNumber = meterId.HasValue && metersById.TryGetValue(meterId.Value, out var meter) ? meter.SerialNumber : null;

            chains[c.Id] = chain;
            rows.Add(new ConsumerMasterRow(
                c.Id, c.AccountNumber, c.Name, c.RrNumber, meterNumber,
                dt?.Code, feeder?.Code, substation?.Code,
                chain.Region?.Name, chain.Zone?.Name, chain.Circle?.Name, chain.Division?.Name, chain.SubDivision?.Name, chain.Section?.Name,
                sp?.Address ?? "", c.MobileNumber, c.ConnectionStatus, c.ServiceDate,
                c.SanctionedLoadKw, c.ContractDemandKva, c.ConnectedLoadKw,
                c.LoadType, c.TariffCategoryCode, c.CommunicationType, c.PaymentMode,
                c.IsNetMeter, c.BillDay, c.BillCycle, c.Latitude, c.Longitude,
                c.MeterMake, c.MeterPhase, c.MultiplyingFactor, c.IsMrRequiredDone, c.Satno, c.MdmAssetTimestampUtc, c.MeterReplacementDate));
        }

        IEnumerable<ConsumerMasterRow> filtered = rows;
        if (orgUnitId.HasValue)
        {
            filtered = filtered.Where(r => chains[r.Id].Contains(orgUnitId.Value));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            filtered = filtered.Where(r =>
                r.AccountNumber.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                r.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (r.RrNumber?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.MeterNumber?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var ordered = filtered.OrderBy(r => r.AccountNumber).ToList();
        var total = ordered.Count;

        if (export == "csv")
        {
            var csv = CsvWriter.Write(
                ["Account Number", "Name", "RR Number", "Meter Number", "DTR Code", "Feeder Code", "Substation Code",
                 "Region", "Zone", "Circle", "Division", "Sub Division", "Section", "Address", "Mobile Number", "Connection Status",
                 "Service Date", "Sanctioned Load (kW)", "Contract Demand (kVA)", "Connected Load (kW)", "Load Type",
                 "Tariff Category", "Communication", "Payment Mode", "Net Meter", "Bill Day", "Bill Cycle", "Latitude", "Longitude",
                 "Meter Make", "Phase", "MF", "MR Required Done", "Satno", "MDM Asset Timestamp (UTC)", "Meter Replacement Date"],
                ordered, r => [r.AccountNumber, r.Name, r.RrNumber, r.MeterNumber, r.DtrCode, r.FeederCode, r.SubstationCode,
                    r.Region, r.Zone, r.Circle, r.Division, r.SubDivision, r.Section, r.Address, r.MobileNumber, r.ConnectionStatus,
                    r.ServiceDate?.ToString("yyyy-MM-dd"), r.SanctionedLoadKw, r.ContractDemandKva, r.ConnectedLoadKw, r.LoadType,
                    r.TariffCategoryCode, r.CommunicationType, r.PaymentMode, r.IsNetMeter, r.BillDay, r.BillCycle, r.Latitude, r.Longitude,
                    r.MeterMake, r.MeterPhase, r.MultiplyingFactor, r.IsMrRequiredDone, r.Satno, r.MdmAssetTimestampUtc, r.MeterReplacementDate?.ToString("yyyy-MM-dd")]);
            return File(csv, "text/csv", $"MDMS_Consumers_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        var pageRows = ordered.Skip((p - 1) * size).Take(size).ToList();
        return Ok(new ListResult<ConsumerMasterRow>(pageRows, ReportPaging.BuildInfo(p, size, total), DateTime.UtcNow));
    }

    // --------------------------------------------------------------------- Consumer app self-service

    public record ConsumerSummaryResponse(
        Guid Id, string AccountNumber, string Name, string? MeterNumber,
        decimal? WalletBalance, bool? IsConnected);

    /// <summary>The consumer-app dashboard's single summary call: consumer identity, currently
    /// assigned meter's serial number, and prepaid wallet balance if one exists � real data only,
    /// nulls where a fact genuinely isn't available yet (no prepaid account opened, no meter
    /// assigned) rather than a fabricated placeholder.</summary>
    [HttpGet("{id:guid}/summary")]
    public async Task<IActionResult> GetConsumerSummary(Guid id, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null) return NotFound();

        var sp = await _db.ServicePoints.FirstOrDefaultAsync(x => x.CustomerId == id, ct);
        string? meterNumber = null;
        if (sp is not null)
        {
            var assignment = await _db.MeterAssignments
                .Where(a => a.ServicePointId == sp.Id && a.EffectiveToUtc == null)
                .OrderByDescending(a => a.EffectiveFromUtc)
                .FirstOrDefaultAsync(ct);
            if (assignment is not null)
                meterNumber = (await _db.Meters.FirstOrDefaultAsync(m => m.Id == assignment.MeterId, ct))?.SerialNumber;
        }

        var account = await _db.PrepaidAccounts.FirstOrDefaultAsync(a => a.CustomerId == id, ct);

        return Ok(new ConsumerSummaryResponse(customer.Id, customer.AccountNumber, customer.Name, meterNumber, account?.Balance, account?.IsConnected));
    }

    public record DailyConsumptionRow(DateOnly Date, decimal ConsumptionKwh);

    /// <summary>Last <paramref name="days"/> of Daily Load Profile consumption for this consumer's
    /// currently assigned meter, for the mobile app's Consumption module. Empty (not fabricated)
    /// when no meter is assigned or no DP rows exist yet.</summary>
    [HttpGet("{id:guid}/consumption/daily")]
    public async Task<IActionResult> GetDailyConsumption(Guid id, [FromQuery] int days, CancellationToken ct)
    {
        var take = days is > 0 and <= 366 ? days : 30;
        var sp = await _db.ServicePoints.FirstOrDefaultAsync(x => x.CustomerId == id, ct);
        if (sp is null) return Ok(Array.Empty<DailyConsumptionRow>());

        var assignment = await _db.MeterAssignments
            .Where(a => a.ServicePointId == sp.Id && a.EffectiveToUtc == null)
            .OrderByDescending(a => a.EffectiveFromUtc)
            .FirstOrDefaultAsync(ct);
        if (assignment is null) return Ok(Array.Empty<DailyConsumptionRow>());

        var rows = await _db.DailyLoadProfiles
            .Where(d => d.MeterId == assignment.MeterId)
            .OrderByDescending(d => d.ProfileDate)
            .Take(take)
            .Select(d => new DailyConsumptionRow(d.ProfileDate, d.ConsumptionKwh))
            .ToListAsync(ct);

        rows.Reverse();
        return Ok(rows);
    }

    public record AddServicePointRequest(string Address, Guid? DistributionTransformerNodeId);

    [HttpPost("{id:guid}/service-points")]
    public async Task<IActionResult> AddServicePoint(Guid id, [FromBody] AddServicePointRequest request, CancellationToken ct)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == id, ct);
        if (!customerExists) return NotFound();

        ServicePoint servicePoint;
        try
        {
            servicePoint = new ServicePoint(id, request.Address);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        if (request.DistributionTransformerNodeId is Guid dtId)
        {
            var dtNode = await _db.HierarchyNodes.FirstOrDefaultAsync(n => n.Id == dtId, ct);
            if (dtNode is null) return NotFound($"Distribution transformer node {dtId} not found.");
            try
            {
                servicePoint.AssignDistributionTransformer(dtNode);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        _db.ServicePoints.Add(servicePoint);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id }, new ServicePointSummary(servicePoint.Id, servicePoint.Address, servicePoint.DistributionTransformerNodeId));
    }

    [HttpPost("{id:guid}/service-points/{servicePointId:guid}/assign-dt/{dtNodeId:guid}")]
    public async Task<IActionResult> AssignDistributionTransformer(Guid id, Guid servicePointId, Guid dtNodeId, CancellationToken ct)
    {
        var servicePoint = await _db.ServicePoints.FirstOrDefaultAsync(sp => sp.Id == servicePointId && sp.CustomerId == id, ct);
        if (servicePoint is null) return NotFound();

        var dtNode = await _db.HierarchyNodes.FirstOrDefaultAsync(n => n.Id == dtNodeId, ct);
        if (dtNode is null) return NotFound($"Distribution transformer node {dtNodeId} not found.");

        try
        {
            servicePoint.AssignDistributionTransformer(dtNode);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new ServicePointSummary(servicePoint.Id, servicePoint.Address, servicePoint.DistributionTransformerNodeId));
    }
}
