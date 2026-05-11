using DotnetNiger.Identity.Infrastructure;

namespace DotnetNiger.Identity.Infrastructure;

/// <summary>
/// Résout le TenantId courant depuis le JWT ou le header X-Tenant-Id.
/// Les query filters du DbContext utilisent cette valeur pour isoler les données.
/// </summary>
public class TenantResolutionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantContext _tenantContext;

    public TenantResolutionService(IHttpContextAccessor httpContextAccessor, TenantContext tenantContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    public void ResolveTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // 1. Depuis le JWT (claim "tenant_id")
        var user = httpContext.User;
        var tenantClaim = user.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantClaim, out var tenantId))
        {
            _tenantContext.TenantId = tenantId;
            return;
        }

        // 2. Depuis le header X-Tenant-Id (pour les endpoints publics)
        var headerTenant = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (Guid.TryParse(headerTenant, out var headerTid))
            _tenantContext.TenantId = headerTid;
    }
}
