using MDMS.Api.Tenancy;
using MDMS.Application.Common;
using MDMS.Application.Prepaid;
using MDMS.Application.Security;
using MDMS.Domain.Entities;
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
    private readonly PrepaidService _prepaid;
    private readonly HttpTenantContext _tenantContext;

    public ConsumerAuthController(IMdmsDbContext db, JwtTokenService tokens, PrepaidService prepaid, HttpTenantContext tenantContext)
    {
        _db = db;
        _tokens = tokens;
        _prepaid = prepaid;
        _tenantContext = tenantContext;
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

    public record LinkedAccountRow(string AccountNumber, string Name, string TenantName);

    /// <summary>
    /// Every account registered against a mobile number, across every organisation (tenant) --
    /// backs the app's "Add Account" flow (a consumer can link another connection billed to the
    /// same phone) and doubles as "Add Organisation": an account found here in a different
    /// tenant than the caller's current one is simply a different organisation to switch into,
    /// since each linked account's own login token already carries its own tenant claim.
    /// </summary>
    [HttpGet("accounts")]
    [AllowAnonymous]
    public async Task<IActionResult> ListAccountsByMobile([FromQuery] string mobileNumber, CancellationToken ct)
    {
        var mobile = mobileNumber?.Trim();
        if (string.IsNullOrWhiteSpace(mobile))
            return BadRequest("Mobile number is required.");

        var customers = await _db.Customers.IgnoreQueryFilters().Where(c => c.MobileNumber == mobile).ToListAsync(ct);
        var tenantIds = customers.Select(c => c.TenantId).Distinct().ToList();
        var tenants = await _db.Tenants.IgnoreQueryFilters().Where(t => tenantIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        return Ok(customers.Select(c => new LinkedAccountRow(c.AccountNumber, c.Name, tenants.GetValueOrDefault(c.TenantId, "Default Organisation"))).ToList());
    }

    /// <summary>
    /// Fixed placeholder OTP used by the register/forgot-password flows below until a real
    /// email/SMS provider is configured (this project has none -- see this controller's own doc
    /// comment). Every request-otp response discloses this in <c>OtpDeliveryNote</c> so the app
    /// can show the consumer the real state of affairs instead of pretending a code was sent.
    /// Swap in a real IOtpSender + persisted, expiring challenge once SMTP/SMS is wired up.
    /// </summary>
    private const string DevPlaceholderOtp = "1234";
    private const string DevOtpNote = "Development mode: no email/SMS provider is configured yet, so no code was actually sent. Use 1234 to continue.";

    public record RequestOtpRequest(string AccountNumber, string MobileNumber);
    public record RequestOtpResponse(bool Sent, string OtpDeliveryNote);

    /// <summary>Shared identity check for both registration and password-reset OTP requests --
    /// same real match (account number + mobile number on file) Login already uses.</summary>
    private async Task<Customer?> VerifyIdentityAsync(string? accountNumber, string? mobileNumber, CancellationToken ct)
    {
        var acc = accountNumber?.Trim();
        var mobile = mobileNumber?.Trim();
        if (string.IsNullOrWhiteSpace(acc) || string.IsNullOrWhiteSpace(mobile)) return null;

        var customer = await _db.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.AccountNumber == acc, ct);
        return customer is not null && customer.MobileNumber == mobile ? customer : null;
    }

    [HttpPost("register/request-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestRegisterOtp([FromBody] RequestOtpRequest request, CancellationToken ct)
    {
        var customer = await VerifyIdentityAsync(request.AccountNumber, request.MobileNumber, ct);
        if (customer is null) return Unauthorized("Account number and mobile number do not match our records.");
        if (customer.PasswordHash is not null) return Conflict("This account already has a password set. Use Login instead.");

        return Ok(new RequestOtpResponse(true, DevOtpNote));
    }

    public record CompleteRegisterRequest(string AccountNumber, string MobileNumber, string Otp, string Password);

    [HttpPost("register/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteRegister([FromBody] CompleteRegisterRequest request, CancellationToken ct)
    {
        var customer = await VerifyIdentityAsync(request.AccountNumber, request.MobileNumber, ct);
        if (customer is null) return Unauthorized("Account number and mobile number do not match our records.");
        if (customer.PasswordHash is not null) return Conflict("This account already has a password set. Use Login instead.");
        if (request.Otp?.Trim() != DevPlaceholderOtp) return BadRequest("Incorrect verification code.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6) return BadRequest("Password must be at least 6 characters.");

        customer.SetPasswordHash(PasswordHasher.Hash(request.Password));
        await _db.SaveChangesAsync(ct);

        var token = _tokens.CreateConsumerAccessToken(customer.Id, customer.Name, customer.TenantId);
        return Ok(new ConsumerLoginResponse(token.Value, token.ExpiresAtUtc, customer.Id, customer.Name, customer.AccountNumber));
    }

    public record PasswordLoginRequest(string AccountNumber, string Password);

    [HttpPost("password/login")]
    [AllowAnonymous]
    public async Task<IActionResult> PasswordLogin([FromBody] PasswordLoginRequest request, CancellationToken ct)
    {
        var accountNumber = request.AccountNumber?.Trim();
        if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Account number and password are required.");

        var customer = await _db.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.AccountNumber == accountNumber, ct);
        if (customer?.PasswordHash is null || !PasswordHasher.Verify(customer.PasswordHash, request.Password))
            return Unauthorized("Incorrect account number or password.");

        var token = _tokens.CreateConsumerAccessToken(customer.Id, customer.Name, customer.TenantId);
        return Ok(new ConsumerLoginResponse(token.Value, token.ExpiresAtUtc, customer.Id, customer.Name, customer.AccountNumber));
    }

    [HttpPost("password/forgot/request-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestForgotPasswordOtp([FromBody] RequestOtpRequest request, CancellationToken ct)
    {
        var customer = await VerifyIdentityAsync(request.AccountNumber, request.MobileNumber, ct);
        if (customer is null) return Unauthorized("Account number and mobile number do not match our records.");

        return Ok(new RequestOtpResponse(true, DevOtpNote));
    }

    public record CompleteForgotPasswordRequest(string AccountNumber, string MobileNumber, string Otp, string NewPassword);

    [HttpPost("password/forgot/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteForgotPassword([FromBody] CompleteForgotPasswordRequest request, CancellationToken ct)
    {
        var customer = await VerifyIdentityAsync(request.AccountNumber, request.MobileNumber, ct);
        if (customer is null) return Unauthorized("Account number and mobile number do not match our records.");
        if (request.Otp?.Trim() != DevPlaceholderOtp) return BadRequest("Incorrect verification code.");
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6) return BadRequest("Password must be at least 6 characters.");

        customer.SetPasswordHash(PasswordHasher.Hash(request.NewPassword));
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    [HttpPost("password/change")]
    [Authorize(Roles = "Consumer")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var consumerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(consumerId, out var id)) return Unauthorized();

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer?.PasswordHash is null || !PasswordHasher.Verify(customer.PasswordHash, request.CurrentPassword))
            return Unauthorized("Current password is incorrect.");
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6) return BadRequest("Password must be at least 6 characters.");

        customer.SetPasswordHash(PasswordHasher.Hash(request.NewPassword));
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    public record GuestRechargeRequest(string AccountNumber, string MobileNumber, decimal Amount, string Reference);
    public record GuestRechargeResponse(string AccountNumber, decimal Amount, decimal BalanceAfter, DateTime CreatedAtUtc);

    /// <summary>
    /// Recharge without signing in -- verifies the account number against its own registered
    /// mobile number (same check as Login) before crediting, so a guest can only recharge an
    /// account whose phone number they can actually provide, and never needs a session to do it.
    /// </summary>
    [HttpPost("guest-recharge")]
    [AllowAnonymous]
    public async Task<IActionResult> GuestRecharge([FromBody] GuestRechargeRequest request, CancellationToken ct)
    {
        var accountNumber = request.AccountNumber?.Trim();
        var mobile = request.MobileNumber?.Trim();
        if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(mobile))
            return BadRequest("Account number and registered mobile number are required.");

        var customer = await _db.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.AccountNumber == accountNumber, ct);
        if (customer is null || customer.MobileNumber != mobile)
            return Unauthorized("Account number and mobile number do not match our records.");

        // An anonymous request carries no tenant by default (see MdmsDbContext's query filter);
        // set it to the verified customer's own organisation so PrepaidService's tenant-scoped
        // reads/writes land on their real account instead of missing it and creating a duplicate.
        _tenantContext.Set(customer.TenantId);

        try
        {
            var transaction = await _prepaid.RechargeAsync(customer.Id, request.Amount, request.Reference, ct);
            return Ok(new GuestRechargeResponse(customer.AccountNumber, transaction.Amount, transaction.BalanceAfter, transaction.CreatedAtUtc));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
