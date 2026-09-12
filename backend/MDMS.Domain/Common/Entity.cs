namespace MDMS.Domain.Common;

/// <summary>Base type for every MDMS domain entity: a GUID identity, tenant scope, and creation bookkeeping.</summary>
public abstract class Entity
{
    /// <summary>
    /// Well-known tenant id used until a real multi-tenant resolver (the future `iam` module)
    /// assigns one per request. Every entity carries <see cref="TenantId"/> from day one so no
    /// table needs retrofitting later — only the *value* changes once tenant context exists.
    /// </summary>
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>The owning DISCOM. Defaults to <see cref="DefaultTenantId"/> until tenant context is wired in.</summary>
    public Guid TenantId { get; protected set; } = DefaultTenantId;

    /// <summary>UTC timestamp the row was first persisted. Never mutated after insert.</summary>
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Reassigns the owning tenant. Exists for the future tenant-resolution layer to call after
    /// construction (e.g. from a request-scoped tenant context) — no current caller uses this yet.
    /// </summary>
    public void AssignTenant(Guid tenantId) => TenantId = tenantId;
}
