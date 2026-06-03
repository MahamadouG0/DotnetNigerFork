using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using DotnetNiger.Identity.Domain.Entities;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class OidcController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public OidcController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("~/connect/logout"), HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        var postLogoutUri = Request.HasFormContentType
            ? Request.Form["post_logout_redirect_uri"].FirstOrDefault()
            : Request.Query["post_logout_redirect_uri"].FirstOrDefault();

        if (User.Identity?.IsAuthenticated == true)
            await _signInManager.SignOutAsync();

        return SignOut(
            authenticationSchemes: new[]
            {
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                IdentityConstants.ApplicationScheme
            },
            properties: new AuthenticationProperties
            {
                RedirectUri = postLogoutUri
            });
    }
}
