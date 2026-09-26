namespace MDMS.Domain.Common;

/// <summary>Marks an entity that is NOT scoped to the active organisation (tenant): the tenant
/// registry itself, cross-tenant user access grants, and auth refresh tokens. Every other Entity
/// is filtered to the active tenant and stamped with it on insert.</summary>
public interface ITenantExempt { }
