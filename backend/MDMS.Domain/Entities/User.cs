using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// A named user of this MDMS with a fixed <see cref="Role"/> and, for most roles, a scoping
/// <see cref="OrgUnitId"/> (field roles are constrained by organizational geography, not only by
/// role — an "Admin" is the one role meaningfully unscoped). <see cref="PasswordHash"/> is null
/// until the user claims their account (see AuthController's claim endpoint) — until then the
/// account exists as a record (creatable by an Admin) but cannot sign in.
/// </summary>
public class User : Entity
{
    public string Username { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public UserRole Role { get; private set; }

    /// <summary>The org unit this user's access/work is scoped to. Null only makes sense for an unscoped role like Admin.</summary>
    public Guid? OrgUnitId { get; private set; }

    /// <summary>
    /// PBKDF2 password hash (see MDMS.Application.Security.PasswordHasher) — never the plaintext
    /// password. Null means the account has not been claimed yet and cannot sign in.
    /// </summary>
    public string? PasswordHash { get; private set; }

    private User() { }

    public User(string username, string displayName, UserRole role, Guid? orgUnitId)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));
        if (role != UserRole.Admin && orgUnitId is null)
        {
            throw new ArgumentException(
                $"{role} must be scoped to an OrgUnit; only {UserRole.Admin} may be unscoped.", nameof(orgUnitId));
        }

        Username = username;
        DisplayName = displayName;
        Role = role;
        OrgUnitId = orgUnitId;
    }

    public void Reassign(UserRole role, Guid? orgUnitId)
    {
        if (role != UserRole.Admin && orgUnitId is null)
        {
            throw new ArgumentException(
                $"{role} must be scoped to an OrgUnit; only {UserRole.Admin} may be unscoped.", nameof(orgUnitId));
        }

        Role = role;
        OrgUnitId = orgUnitId;
    }

    /// <summary>Sets the password hash. Callers are responsible for hashing — this never sees plaintext.</summary>
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        PasswordHash = passwordHash;
    }
}
