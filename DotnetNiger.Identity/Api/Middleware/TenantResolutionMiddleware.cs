using DotnetNiger.Identity.Infrastructure;

namespace DotnetNiger.Identity.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, TenantResolutionService resolver)
    {
        resolver.ResolveTenant();
        await _next(context);
    }
}
