using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace DotnetNiger.Identity.Infrastructure;

public class RoleClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.HasClaim(c => c.Type == ClaimTypes.Role))
            return Task.FromResult(principal);

        var roleClaims = principal.FindAll("role").ToList();
        if (roleClaims.Count == 0)
            return Task.FromResult(principal);

        var identity = new ClaimsIdentity("OpenIddict");
        foreach (var claim in roleClaims)
            identity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));

        principal.AddIdentity(identity);
        return Task.FromResult(principal);
    }
}
