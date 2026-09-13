using MDMS.Application.Common;
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

    public record AddServicePointRequest(string Address);

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

        _db.ServicePoints.Add(servicePoint);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id }, new ServicePointSummary(servicePoint.Id, servicePoint.Address, servicePoint.DistributionTransformerNodeId));
    }
}
