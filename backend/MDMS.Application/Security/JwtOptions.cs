namespace MDMS.Application.Security;

/// <summary>
/// Bound from configuration section "Jwt". <see cref="SigningKey"/> MUST be overridden outside
/// Development (environment variable / secret store) — the value in appsettings.json is a
/// clearly-marked development-only placeholder, the same pattern already used for the Postgres
/// connection string's placeholder password.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "mdms";
    public string Audience { get; set; } = "mdms-frontend";
    public string SigningKey { get; set; } = default!;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
}
