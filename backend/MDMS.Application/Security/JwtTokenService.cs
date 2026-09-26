using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MDMS.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MDMS.Application.Security;

public record AccessToken(string Value, DateTime ExpiresAtUtc);

/// <summary>
/// Issues short-lived JWT access tokens (HMAC-SHA256-signed, claims: sub/name/role/tenant) and
/// opaque refresh token values. The access token is meant to live in the frontend's memory only
/// (never localStorage); the refresh token is meant to be set as an HttpOnly cookie by the
/// caller — this service only creates the values, it doesn't know about cookies or HTTP.
/// </summary>
public class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    /// <summary>
    /// Issues an access token for a Consumer (mobile app) session � not a <see cref="User"/> (staff
    /// account). Carries the Customer id as the subject and a fixed "Consumer" role claim, so
    /// existing [Authorize] endpoints accept it exactly like a staff token, while the mobile app's
    /// own backend calls scope every query to this consumerId.
    /// </summary>
    public AccessToken CreateConsumerAccessToken(Guid customerId, string displayName, Guid tenantId)
    {
        var expiresAtUtc = DateTime.UtcNow.AddDays(30); // consumer app sessions are long-lived; refreshed via /consumer-auth/login again
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, customerId.ToString()),
            new Claim("name", displayName),
            new Claim(ClaimTypes.Role, "Consumer"),
            new Claim("tenant", tenantId.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer, audience: _options.Audience, claims: claims, expires: expiresAtUtc, signingCredentials: credentials);
        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public AccessToken CreateAccessToken(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("name", user.DisplayName),
            new Claim("username", user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("tenant", user.TenantId.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    /// <summary>A cryptographically random opaque refresh token — not a JWT, just an unguessable
    /// value. Only its hash (see <see cref="Hash"/>) is ever persisted.</summary>
    public static string GenerateRefreshTokenValue() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    /// <summary>Refresh tokens are hashed with plain SHA-256 (not PBKDF2): unlike a password, this
    /// value is already 512 bits of random entropy, not attacker-guessable, so a slow KDF buys
    /// nothing here and would only slow down every refresh request.</summary>
    public static string Hash(string refreshTokenValue)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshTokenValue));
        return Convert.ToBase64String(bytes);
    }

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);
}
