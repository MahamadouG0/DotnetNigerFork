using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace DotnetNiger.Identity.Api.Authentication;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class InternalApiKeyAuthAttribute : Attribute, IAuthorizationFilter
{
    private const string InternalKeyHeader = "X-Internal-Key";
    private static string? _configuredKey;

    public InternalApiKeyAuthAttribute() { }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (_configuredKey == null)
        {
            var configuration = context.HttpContext.RequestServices.GetService<IConfiguration>();
            _configuredKey = configuration?["InternalApiKey"] ?? "";
        }

        if (string.IsNullOrEmpty(_configuredKey))
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(InternalKeyHeader, out var values))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (values.FirstOrDefault() != _configuredKey)
            context.Result = new UnauthorizedResult();
    }
}
