using MDMS.Domain.Common;

namespace MDMS.Domain.Entities;

/// <summary>
/// A DISCOM (Distribution Company) — the tenant boundary every other table scopes to via
/// <see cref="Entity.TenantId"/>. Only one row (<see cref="Entity.DefaultTenantId"/>) is expected
/// to exist until a real onboarding flow for additional tenants is built.
/// </summary>
public class Tenant : Entity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    private Tenant() { }

    public Tenant(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Code = code;
        Name = name;
    }
}
