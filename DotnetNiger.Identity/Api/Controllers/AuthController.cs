using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Services;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(AuthService authService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _authService = authService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("~/connect/token"), IgnoreAntiforgeryToken, Produces("application/json")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> TokenExchange()
    {
        var grantType = Request.Form["grant_type"].FirstOrDefault();

        if (grantType == "refresh_token")
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (result?.Principal == null)
                throw new InvalidOperationException("The refresh token is invalid");

            var userId = result.Principal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
            if (userId != null)
            {
                var refreshUser = await _userManager.FindByIdAsync(userId);
                if (refreshUser == null || !refreshUser.IsActive)
                    throw new InvalidOperationException("User no longer exists or is inactive");
            }

            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (grantType != "password")
            throw new InvalidOperationException("Unsupported grant type");

        var (loginUser, roles) = await _authService.ValidateCredentialsAsync(
            Request.Form["username"]!, Request.Form["password"]!, null);

        var loginPrincipal = await _signInManager.CreateUserPrincipalAsync(loginUser);
        loginPrincipal.SetClaim(OpenIddictConstants.Claims.Subject, loginUser.Id.ToString());
        foreach (var role in roles)
        {
            loginPrincipal.SetClaim(ClaimTypes.Role, role);
            loginPrincipal.SetClaim("role", role);
        }
        loginPrincipal.SetClaim("tenant_id", loginUser.TenantId.ToString());
        loginPrincipal.SetClaim(OpenIddictConstants.Claims.GivenName, loginUser.FirstName);
        loginPrincipal.SetClaim(OpenIddictConstants.Claims.FamilyName, loginUser.LastName);
        loginPrincipal.SetClaim(OpenIddictConstants.Claims.Name, $"{loginUser.FirstName} {loginUser.LastName}".Trim());
        loginPrincipal.SetClaim(OpenIddictConstants.Claims.Email, loginUser.Email);
        var scopes = Request.Form["scope"];
        loginPrincipal.SetScopes(scopes.Count > 0
            ? scopes.SelectMany(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            : ["openid", "profile", "email", "roles"]);

        var rememberMe = string.Equals(Request.Form["remember_me"].FirstOrDefault(), "true",
            StringComparison.OrdinalIgnoreCase);
        loginPrincipal.SetAccessTokenLifetime(
            rememberMe ? TimeSpan.FromDays(7) : TimeSpan.FromHours(1));

        loginPrincipal.SetDestinations(claim => claim.Type switch
        {
            OpenIddictConstants.Claims.Subject
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Email
                or OpenIddictConstants.Claims.GivenName or OpenIddictConstants.Claims.FamilyName
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            ClaimTypes.Role or "role"
                => [OpenIddictConstants.Destinations.AccessToken],
            "tenant_id"
                => [OpenIddictConstants.Destinations.AccessToken],
            _ => [OpenIddictConstants.Destinations.AccessToken]
        });

        return SignIn(loginPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserInfoResponse>> Login([FromBody] LoginRequest request)
    {
        var (user, roles) = await _authService.ValidateCredentialsAsync(
            request.Email, request.Password, request.TenantId);

        return Ok(new UserInfoResponse(
            user.Id, user.Email!, user.FirstName, user.LastName, user.AvatarUrl,
            user.TenantId, user.IsActive, roles, new List<string>(), request.RememberMe));
    }

    [HttpPost("register")]
    public async Task<ActionResult<object>> Register([FromBody] RegisterRequest request)
    {
        var (user, code) = await _authService.RegisterAsync(
            request.Email, request.Password, request.FirstName, request.LastName, request.TenantId);

        return Ok(new
        {
            message = "Compte créé. Un code de confirmation vous a été envoyé par email.",
            userId = user.Id,
            email = user.Email,
            code = string.IsNullOrEmpty(HttpContext.RequestServices
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<Infrastructure.SmtpOptions>>().Value.Host)
                ? code : null
        });
    }

    [HttpPost("confirm-email")]
    public async Task<ActionResult<object>> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        await _authService.ConfirmEmailAsync(request.Email, request.Code);
        return Ok(new { message = "Email confirmé avec succès. Vous pouvez maintenant vous connecter." });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmailGet([FromQuery] string email, [FromQuery] string code)
    {
        await _authService.ConfirmEmailAsync(email, code);
        return Content("<html><body style='font-family:Segoe UI;text-align:center;padding:60px;background:#f2f2f2'>"
            + "<div style='max-width:480px;margin:auto;background:white;border-radius:8px;padding:40px;box-shadow:0 2px 8px rgba(0,0,0,0.08)'>"
            + "<h1 style='color:#512BD4'>DotnetNiger</h1>"
            + "<p style='font-size:18px;color:#333'>Votre email a été confirmé avec succès !</p>"
            + "<p style='color:#666'>Vous pouvez fermer cette fenêtre et vous connecter.</p>"
            + "</div></body></html>", "text/html");
    }

    [HttpPost("resend-code")]
    public async Task<ActionResult<object>> ResendCode([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ResendConfirmationCodeAsync(request.Email);
        return Ok(new { message = "Un nouveau code de confirmation vous a été envoyé." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Déconnecté" });
    }

    [HttpGet("external-login")]
    public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string? returnUrl)
    {
        var redirectUrl = Url.Action(nameof(ExternalCallback), new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet("external-callback")]
    public async Task<ActionResult<UserInfoResponse>> ExternalCallback(
        [FromQuery] string? returnUrl = null, [FromQuery] bool rememberMe = false)
    {
        var result = await _authService.HandleExternalLoginAsync("external");
        return Ok(new UserInfoResponse(
            result.user.Id, result.user.Email!, result.user.FirstName, result.user.LastName,
            result.user.AvatarUrl, result.user.TenantId, result.user.IsActive,
            result.roles, new List<string>(), rememberMe));
    }

    [HttpGet("userinfo")]
    [Authorize]
    public async Task<ActionResult<UserInfoResponse>> UserInfo()
    {
        var userId = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserInfoResponse(
            user.Id, user.Email!, user.FirstName, user.LastName, user.AvatarUrl,
            user.TenantId, user.IsActive, roles, new List<string>()));
    }
}
