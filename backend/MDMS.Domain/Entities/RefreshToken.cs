using MDMS.Domain.Common;

namespace MDMS.Domain.Entities;

/// <summary>
/// A refresh token issued at login, kept as an HttpOnly cookie on the client. Only a hash of the
/// token is stored (never the raw value) so a database read alone can't be used to impersonate a
/// session — the same reasoning as a password hash. Rotated on every use: <see cref="Revoke"/> is
/// called on the old row and a new row is inserted, so a stolen-and-replayed old token is
/// detectable (its row is already revoked).
/// </summary>
public class RefreshToken : Entity, ITenantExempt
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

    public void Revoke() => RevokedAtUtc = DateTime.UtcNow;
}
