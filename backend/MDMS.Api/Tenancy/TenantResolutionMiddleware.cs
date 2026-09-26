using MDMS.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Tenancy;

/// <summary>
/// Resolves which organisation an authenticated request operates on: the caller's home tenant
/// (JWT "tenant" claim) unless they send an <c>X-Tenant-Id</c> header naming an organisation
/// they have been granted access to. A header naming an organisation they do NOT have access to
/// is ignored (falls back to home) rather than honoured — a client can never widen its own
/// access. The effective tenant is echoed in <c>X-Active-Tenant</c>.
/// </summary>
public class TenantResolutionMiddleware
{
    public const string HeaderName = "X-Tenant-Id";
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext http, HttpTenantContext tenantContext, IMdmsDbContext db)
    {
        if (http.User.Identity?.IsAuthenticated == true
            && Guid.TryParse(http.User.FindFirst("tenant")?.Value, out var home)
            && Guid.TryParse(http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? http.User.FindFirst("sub")?.Value, out var userId))
        {
            var effective = home;
            if (Guid.TryParse(http.Request.Headers[HeaderName].FirstOrDefault(), out var requested) && requested != home
                && await db.UserTenantAccesses.AnyAsync(a => a.UserId == userId && a.GrantedTenantId == requested, http.RequestAborted))
            {
                effective = requested;
            }

            tenantContext.Set(effective);
            http.Response.Headers["X-Active-Tenant"] = effective.ToString();
        }

        await _next(http);
    }
}
