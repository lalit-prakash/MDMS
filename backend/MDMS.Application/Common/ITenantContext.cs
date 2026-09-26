namespace MDMS.Application.Common;

/// <summary>The organisation (tenant) the current request operates on. Null when there is no
/// authenticated request (e.g. the anonymous login endpoints, or a background job) — in which
/// case tenant-scoped queries match nothing unless they explicitly opt out of the filter.</summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
}
