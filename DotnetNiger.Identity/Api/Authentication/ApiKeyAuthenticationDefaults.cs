using Microsoft.AspNetCore.Authentication;

namespace DotnetNiger.Identity.Api.Authentication;

public static class ApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "ApiKey";
    public static readonly string DisplayName = "API Key";
}
