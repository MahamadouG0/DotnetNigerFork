using Microsoft.AspNetCore.Authentication;

namespace DotnetNiger.Identity.Api.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
}
