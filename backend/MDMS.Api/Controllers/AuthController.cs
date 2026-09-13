using MDMS.Application.Common;
using MDMS.Application.Security;
using MDMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Controllers;

/// <summary>
/// Real authentication: hashed passwords (PasswordHasher, PBKDF2), short-lived JWT access tokens
/// returned in the response body (meant to be kept in the frontend's memory, never localStorage),
/// and a long-lived refresh token set as an HttpOnly cookie (invisible to JS, so an XSS bug can't
/// read it the way it could read a localStorage token). Refresh tokens are rotated on every use —
/// see RefreshToken's own doc comment.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "mdms_refresh";

    private readonly IMdmsDbContext _db;
    private readonly JwtTokenService _tokens;
    private readonly IWebHostEnvironment _env;

    public AuthController(IMdmsDbContext db, JwtTokenService tokens, IWebHostEnvironment env)
    {
        _db = db;
        _tokens = tokens;
        _env = env;
    }

    public record LoginRequest(string Username, string Password);
    public record ClaimRequest(string Username, string Password);

    public record UserSummary(Guid Id, string Username, string DisplayName, string Role);
    public record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, UserSummary User);

    /// <summary>
    /// First-time password set for an account an Admin already created with no credentials yet.
    /// Only works while PasswordHash is still null — cannot be used to hijack an already-claimed
    /// account. This is the bootstrap out of the chicken-and-egg problem of needing to be
    /// authenticated to set a password, with no one authenticated yet.
    /// </summary>
    [HttpPost("claim")]
    [AllowAnonymous]
    public async Task<IActionResult> Claim([FromBody] ClaimRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username and password are required.");
        if (request.Password.Length < 8)
            return BadRequest("Password must be at least 8 characters.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null)
            return NotFound("No MDMS user account found with that username.");
        if (user.PasswordHash is not null)
            return Conflict("This account already has a password set. Use Sign in instead.");

        user.SetPasswordHash(PasswordHasher.Hash(request.Password));
        await _db.SaveChangesAsync(ct);

        return Ok(await IssueSessionAsync(user, ct));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null || user.PasswordHash is null || !PasswordHasher.Verify(user.PasswordHash, request.Password))
        {
            // Same message for "no such user" and "wrong password" — distinguishing them lets an
            // attacker enumerate valid usernames.
            return Unauthorized("Invalid username or password.");
        }

        return Ok(await IssueSessionAsync(user, ct));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var refreshValue) || string.IsNullOrEmpty(refreshValue))
            return Unauthorized("No refresh token.");

        var hash = JwtTokenService.Hash(refreshValue);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null || !existing.IsActive)
        {
            Response.Cookies.Delete(RefreshCookieName);
            return Unauthorized("Refresh token is invalid or expired.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == existing.UserId, ct);
        if (user is null)
        {
            Response.Cookies.Delete(RefreshCookieName);
            return Unauthorized("Account no longer exists.");
        }

        existing.Revoke();
        return Ok(await IssueSessionAsync(user, ct));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var refreshValue) && !string.IsNullOrEmpty(refreshValue))
        {
            var hash = JwtTokenService.Hash(refreshValue);
            var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
            existing?.Revoke();
            await _db.SaveChangesAsync(ct);
        }
        Response.Cookies.Delete(RefreshCookieName);
        return NoContent();
    }

    private async Task<AuthResponse> IssueSessionAsync(User user, CancellationToken ct)
    {
        var access = _tokens.CreateAccessToken(user);

        var refreshValue = JwtTokenService.GenerateRefreshTokenValue();
        var refreshToken = new RefreshToken(user.Id, JwtTokenService.Hash(refreshValue), DateTime.UtcNow.Add(_tokens.RefreshTokenLifetime));
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        Response.Cookies.Append(RefreshCookieName, refreshValue, new CookieOptions
        {
            HttpOnly = true,
            // Secure requires HTTPS; the dev server runs on plain http://localhost, so Secure=true
            // there would silently prevent the browser from ever storing the cookie at all — same
            // dev-vs-prod split already used for CORS and the connection string.
            Secure = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(_tokens.RefreshTokenLifetime),
            Path = "/api/v1/auth",
        });

        return new AuthResponse(
            access.Value,
            access.ExpiresAtUtc,
            new UserSummary(user.Id, user.Username, user.DisplayName, user.Role.ToString()));
    }
}
