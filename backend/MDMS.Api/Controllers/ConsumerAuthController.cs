using MDMS.Application.Common;
using MDMS.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Consumer (mobile app) login — deliberately separate from <c>AuthController</c>, which is for
/// utility staff. A real deployment would verify the mobile number via OTP through an SMS gateway
/// (per the mobile app spec); no SMS gateway exists in this project, so this authenticates by
/// matching the account number to the mobile number already on file for that consumer
/// (<c>Customer.MobileNumber</c>, set via master-data import) — good enough to issue a real,
/// scoped session token without fabricating an OTP flow that doesn't exist.
/// </summary>
[ApiController]
[Route("api/v1/consumer-auth")]
public class ConsumerAuthController : ControllerBase
{
    private readonly IMdmsDbContext _db;
    private readonly JwtTokenService _tokens;

    public ConsumerAuthController(IMdmsDbContext db, JwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public record ConsumerLoginRequest(string AccountNumber, string MobileNumber);
    public record ConsumerLoginResponse(string AccessToken, DateTime ExpiresAtUtc, Guid ConsumerId, string Name, string AccountNumber);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] ConsumerLoginRequest request, CancellationToken ct)
    {
        var accountNumber = request.AccountNumber?.Trim();
        var mobile = request.MobileNumber?.Trim();
        if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(mobile))
            return BadRequest("Account number and registered mobile number are required.");

        var customer = await _db.Customers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.AccountNumber == accountNumber, ct);

        if (customer is null || customer.MobileNumber != mobile)
            return Unauthorized("Account number and mobile number do not match our records.");

        var token = _tokens.CreateConsumerAccessToken(customer.Id, customer.Name, customer.TenantId);
        return Ok(new ConsumerLoginResponse(token.Value, token.ExpiresAtUtc, customer.Id, customer.Name, customer.AccountNumber));
    }
}
