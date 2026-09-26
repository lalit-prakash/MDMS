using MDMS.Application.Common;

namespace MDMS.Api.Tenancy;

/// <summary>Per-request holder of the active organisation, filled in by <see cref="TenantResolutionMiddleware"/>.</summary>
public class HttpTenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void Set(Guid tenantId) => TenantId = tenantId;
}
