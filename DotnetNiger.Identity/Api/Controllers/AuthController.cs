using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;


namespace DotnetNiger.Identity.Api.Controllers;

/// <summary>Authentification OAuth2/OIDC : login, register, token exchange, external providers.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[EnableRateLimiting("Auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    private readonly TenantService _tenantService;

    private readonly TenantClientService _tenantClientService;

    private readonly IEmailSender<ApplicationUser> _emailSender;
    private readonly SmtpOptions _smtp;
    private readonly IMemoryCache _cache;

    public AuthController(AuthService authService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TenantService tenantService,
        TenantClientService tenantClientService,
        IEmailSender<ApplicationUser> emailSender,
        IOptions<SmtpOptions> smtp,
        IMemoryCache cache)
    {
        _authService = authService;
        _userManager = userManager;
        _signInManager = signInManager;
        _tenantService = tenantService;
        _tenantClientService = tenantClientService;
        _emailSender = emailSender;
        _smtp = smtp.Value;
        _cache = cache;
    }

    /// <summary>Point d'entrée OIDC Authorize. Gère le flux authorization_code.</summary>
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("La requête OpenID Connect est introuvable.");

        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!result.Succeeded)
        {
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + Request.QueryString
                });
        }

        var user = await _userManager.GetUserAsync(result.Principal);
        if (user == null || !user.IsActive)
        {
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + Request.QueryString
                });
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        principal.SetClaim(Claims.Subject, user.Id.ToString());

        var scopes = (request.Scope ?? "openid profile email roles offline_access")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        principal.SetScopes(scopes);

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            principal.SetClaim(ClaimTypes.Role, role);
            principal.SetClaim("role", role);
        }
        principal.SetClaim("tenant_id", user.TenantId.ToString());
        principal.SetClaim(Claims.GivenName, user.FirstName);
        principal.SetClaim(Claims.FamilyName, user.LastName);
        principal.SetClaim(Claims.Name, $"{user.FirstName} {user.LastName}".Trim());
        principal.SetClaim(Claims.Email, user.Email);

        principal.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject
                => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Name or Claims.Email
                or Claims.GivenName or Claims.FamilyName
                => [Destinations.AccessToken, Destinations.IdentityToken],
            ClaimTypes.Role or "role"
                => [Destinations.AccessToken, Destinations.IdentityToken],
            "tenant_id"
                => [Destinations.AccessToken],
            _ => [Destinations.AccessToken]
        });

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>Endpoint OAuth2/OIDC token. Supporte password, client_credentials et refresh_token grants.</summary>
    /// <remarks>
    /// Format : `application/x-www-form-urlencoded`
    ///
    /// **Password flow :**
    /// ```
    /// grant_type=password&amp;username=admin@dotnetniger.com&amp;password=Admin%40123456&amp;scope=openid+profile+email+roles+offline_access
    /// ```
    ///
    /// **Client credentials flow :**
    /// ```
    /// grant_type=client_credentials&amp;client_id={client_id}&amp;client_secret={secret}&amp;scope=api
    /// ```
    ///
    /// **Refresh token flow :**
    /// ```
    /// grant_type=refresh_token&amp;refresh_token={token}
    /// ```
    /// </remarks>
    [HttpPost("~/connect/token"), IgnoreAntiforgeryToken, Produces("application/json")]
    public async Task<IActionResult> TokenExchange()
    {
        var grantType = Request.Form["grant_type"].FirstOrDefault();

        if (grantType == "client_credentials")
        {
            var clientId = Request.Form["client_id"].FirstOrDefault();
            if (string.IsNullOrEmpty(clientId))
                throw new InvalidOperationException("client_id is required");

            return await HandleClientCredentialsGrantAsync(clientId);
        }

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

        if (grantType == "authorization_code")
        {
            var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            if (principal == null)
                throw new InvalidOperationException("The authorization code is invalid");
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (grantType != "password")
            throw new InvalidOperationException("Unsupported grant type");

        var (loginUser, roles) = await _authService.ValidateCredentialsAsync(
            Request.Form["username"]!, Request.Form["password"]!, null);

        if (loginUser.TwoFactorEnabled)
        {
            var challengeToken = Guid.NewGuid().ToString("N");
            var cacheEntry = new TwoFactorChallenge(
                loginUser.Id,
                loginUser.Email!,
                loginUser.TenantId,
                DateTime.UtcNow.AddMinutes(5));
            _cache.Set($"2fa_challenge_{challengeToken}", cacheEntry, TimeSpan.FromMinutes(5));

            return Ok(new TwoFactorRequiredResponse(true, challengeToken));
        }

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
            ? scopes.SelectMany(s => (s ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
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
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            "tenant_id"
                => [OpenIddictConstants.Destinations.AccessToken],
            _ => [OpenIddictConstants.Destinations.AccessToken]
        });

        return SignIn(loginPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>Inscription multi-tenant : crée un tenant + admin + client OAuth2 + clé API atomiquement.</summary>
    [HttpPost("register-tenant")]
    [EnableRateLimiting("TenantRegistration")]
    public async Task<ActionResult<RegisterTenantResponse>> RegisterTenant([FromBody] RegisterTenantRequest request)
    {
        var result = await _tenantService.RegisterTenantAsync(request);

        return Ok(result);
    }

    ///<summary>Authentification par email/mot de passe. Retourne les infos utilisateur + rôles.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<UserInfoResponse>> Login([FromBody] LoginRequest request)
    {
        ApplicationUser? user = null;
        try
        {
            (user, var roles) = await _authService.ValidateCredentialsAsync(
                request.Email, request.Password, request.TenantId);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ua = Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";
            await _authService.RecordLoginAsync(user.Id, ip, ua, true);

            return Ok(new UserInfoResponse(
                user.Id, user.Email!, user.FirstName, user.LastName, user.AvatarUrl,
                user.TenantId, user.IsActive, roles, new List<string>(), request.RememberMe));
        }
        catch (UnauthorizedAccessException ex)
        {
            if (user != null)
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var ua = Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";
                await _authService.RecordLoginAsync(user.Id, ip, ua, false, failureReason: ex.Message);
            }
            return Unauthorized(new ErrorResponse(ex.Message));
        }
    }

    /// <summary>Inscription d'un nouvel utilisateur. Un code de confirmation est envoyé par email.</summary>
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

    /// <summary>Confirme l'adresse email avec le code reçu.</summary>
    [HttpPost("confirm-email")]
    public async Task<ActionResult<object>> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        await _authService.ConfirmEmailAsync(request.Email, request.Code);
        return Ok(new { message = "Email confirmé avec succès. Vous pouvez maintenant vous connecter." });
    }

    /// <summary>Confirme l'email via lien (GET). Affiche une page de confirmation.</summary>
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

    /// <summary>Réenvoie le code de confirmation email.</summary>
    [HttpPost("resend-code")]
    public async Task<ActionResult<object>> ResendCode([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ResendConfirmationCodeAsync(request.Email);
        return Ok(new { message = "Un nouveau code de confirmation vous a été envoyé." });
    }

    private async Task<IActionResult> HandleClientCredentialsGrantAsync(string clientId)
    {
        var clients = await _tenantClientService.GetClientsByClientIdAsync(clientId);
        var tenantClient = clients.FirstOrDefault()
            ?? throw new InvalidOperationException("Client non trouvé ou inactif");

        if (!tenantClient.IsActive)
            throw new InvalidOperationException("Ce client est désactivé");

        var tenant = await _tenantService.GetByIdAsync(tenantClient.TenantId);
        if (tenant == null || !tenant.IsActive)
            throw new InvalidOperationException("Le tenant associé à ce client est inactif");

        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.AddClaim(OpenIddictConstants.Claims.Subject, clientId);
        identity.AddClaim(OpenIddictConstants.Claims.Name, tenantClient.ClientName);
        identity.AddClaim("tenant_id", tenantClient.TenantId.ToString());
        identity.AddClaim("client_id", clientId);
        identity.AddClaim(ClaimTypes.Role, "Client");
        identity.AddClaim("role", "Client");

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(Request.Form["scope"].SelectMany(
            s => (s ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)));
        principal.SetDestinations(claim => claim.Type switch
        {
            OpenIddictConstants.Claims.Subject
                => [OpenIddictConstants.Destinations.AccessToken],
            OpenIddictConstants.Claims.Name
                => [OpenIddictConstants.Destinations.AccessToken],
            "tenant_id" or "client_id"
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            ClaimTypes.Role or "role"
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            _ => [OpenIddictConstants.Destinations.AccessToken],
        });

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private bool IsTwoFactorRateLimited(Guid userId)
    {
        var key = $"2fa_attempts_{userId}";
        var attempts = _cache.Get<int>(key);
        if (attempts >= 5)
            return true;
        _cache.Set(key, attempts + 1, TimeSpan.FromMinutes(1));
        return false;
    }

    /// <summary>Vérifie un code 2FA et complète la connexion (password grant).</summary>
    [AllowAnonymous]
    [HttpPost("verify-2fa")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] Verify2faRequest request)
    {
        var userIdClaim = User.FindFirst(Claims.Subject)?.Value ?? request.ChallengeToken ?? string.Empty;
        if (IsTwoFactorRateLimited(Guid.TryParse(userIdClaim, out var uid) ? uid : Guid.Empty))
            return BadRequest(new { error = "Trop de tentatives. Réessayez dans une minute." });

        if (!_cache.TryGetValue($"2fa_challenge_{request.ChallengeToken}", out TwoFactorChallenge? challenge) || challenge == null)
            return BadRequest(new ErrorResponse("Jeton de vérification invalide ou expiré"));

        if (challenge.ExpiresAt < DateTime.UtcNow)
        {
            _cache.Remove($"2fa_challenge_{request.ChallengeToken}");
            return BadRequest(new ErrorResponse("Jeton de vérification expiré"));
        }

        var user = await _userManager.FindByIdAsync(challenge.UserId.ToString());
        if (user == null)
            return BadRequest(new ErrorResponse("Utilisateur non trouvé"));

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, request.Code);

        if (!isValid)
        {
            // Try recovery code
            return BadRequest(new ErrorResponse("Code de vérification invalide"));
        }

        _cache.Remove($"2fa_challenge_{request.ChallengeToken}");

        var roles = await _userManager.GetRolesAsync(user);
        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        principal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
        foreach (var role in roles)
        {
            principal.SetClaim(ClaimTypes.Role, role);
            principal.SetClaim("role", role);
        }
        principal.SetClaim("tenant_id", user.TenantId.ToString());
        principal.SetClaim(OpenIddictConstants.Claims.GivenName, user.FirstName);
        principal.SetClaim(OpenIddictConstants.Claims.FamilyName, user.LastName);
        principal.SetClaim(OpenIddictConstants.Claims.Name, $"{user.FirstName} {user.LastName}".Trim());
        principal.SetClaim(OpenIddictConstants.Claims.Email, user.Email);

        var scopes = Request.Form["scope"].Count > 0
            ? Request.Form["scope"].SelectMany(s => (s ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            : ["openid", "profile", "email", "roles"];
        principal.SetScopes(scopes);

        principal.SetDestinations(claim => claim.Type switch
        {
            OpenIddictConstants.Claims.Subject
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Email
                or OpenIddictConstants.Claims.GivenName or OpenIddictConstants.Claims.FamilyName
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            ClaimTypes.Role or "role"
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            "tenant_id"
                => [OpenIddictConstants.Destinations.AccessToken],
            _ => [OpenIddictConstants.Destinations.AccessToken]
        });

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>Vérifie un code de récupération 2FA et complète la connexion.</summary>
    [AllowAnonymous]
    [HttpPost("verify-2fa-recovery")]
    public async Task<IActionResult> VerifyTwoFactorRecovery([FromBody] TwoFactorRecoveryCodeRequest request)
    {
        var userIdClaim = User.FindFirst(Claims.Subject)?.Value ?? request.ChallengeToken ?? string.Empty;
        if (IsTwoFactorRateLimited(Guid.TryParse(userIdClaim, out var uid) ? uid : Guid.Empty))
            return BadRequest(new { error = "Trop de tentatives. Réessayez dans une minute." });

        if (!_cache.TryGetValue($"2fa_challenge_{request.ChallengeToken}", out TwoFactorChallenge? challenge) || challenge == null)
            return BadRequest(new ErrorResponse("Jeton de vérification invalide ou expiré"));

        if (challenge.ExpiresAt < DateTime.UtcNow)
        {
            _cache.Remove($"2fa_challenge_{request.ChallengeToken}");
            return BadRequest(new ErrorResponse("Jeton de vérification expiré"));
        }

        var user = await _userManager.FindByIdAsync(challenge.UserId.ToString());
        if (user == null)
            return BadRequest(new ErrorResponse("Utilisateur non trouvé"));

        var recoveryResult = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, request.RecoveryCode);
        if (!recoveryResult.Succeeded)
            return BadRequest(new ErrorResponse("Code de récupération invalide"));

        _cache.Remove($"2fa_challenge_{request.ChallengeToken}");

        var roles = await _userManager.GetRolesAsync(user);
        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        principal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
        foreach (var role in roles)
        {
            principal.SetClaim(ClaimTypes.Role, role);
            principal.SetClaim("role", role);
        }
        principal.SetClaim("tenant_id", user.TenantId.ToString());
        principal.SetClaim(OpenIddictConstants.Claims.GivenName, user.FirstName);
        principal.SetClaim(OpenIddictConstants.Claims.FamilyName, user.LastName);
        principal.SetClaim(OpenIddictConstants.Claims.Name, $"{user.FirstName} {user.LastName}".Trim());
        principal.SetClaim(OpenIddictConstants.Claims.Email, user.Email);

        var scopes = Request.Form["scope"].Count > 0
            ? Request.Form["scope"].SelectMany(s => (s ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            : ["openid", "profile", "email", "roles"];
        principal.SetScopes(scopes);

        principal.SetDestinations(claim => claim.Type switch
        {
            OpenIddictConstants.Claims.Subject
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Email
                or OpenIddictConstants.Claims.GivenName or OpenIddictConstants.Claims.FamilyName
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            ClaimTypes.Role or "role"
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            "tenant_id"
                => [OpenIddictConstants.Destinations.AccessToken],
            _ => [OpenIddictConstants.Destinations.AccessToken]
        });

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>Déconnexion de l'utilisateur connecté.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Déconnecté" });
    }

    /// <summary>Redirige vers le fournisseur externe (Google, GitHub, Microsoft).</summary>
    [HttpGet("external-login")]
    public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string? returnUrl)
    {
        var redirectUrl = Url.Action(nameof(ExternalCallback), new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    /// <summary>Callback des fournisseurs OAuth externes. Traite le retour et connecte l'utilisateur.</summary>
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

    /// <summary>Retourne les informations de l'utilisateur connecté (email, rôles, tenant).</summary>
    [HttpGet("userinfo")]
    [Authorize]
    public async Task<ActionResult<UserInfoResponse>> UserInfo()
    {
        var userId = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (userId == null || tenantClaim == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        if (!Guid.TryParse(tenantClaim, out var tokenTenantId) || user.TenantId != tokenTenantId)
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserInfoResponse(
            user.Id, user.Email!, user.FirstName, user.LastName, user.AvatarUrl,
            user.TenantId, user.IsActive, roles, new List<string>()));
    }

    /// <summary>Bootstrap du client OIDC "web-ui" (création ou mise à jour des permissions/URIs).</summary>
    [AllowAnonymous]
    [HttpPost("bootstrap-web-ui")]
    public async Task<IActionResult> BootstrapWebUi(
        [FromServices] IOpenIddictApplicationManager appManager)
    {
        var existing = await appManager.FindByClientIdAsync("web-ui");
        if (existing != null)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await appManager.PopulateAsync(descriptor, existing);
            descriptor.Permissions.Add("ep:token");
            descriptor.Permissions.Add("ep:authorization");
            descriptor.Permissions.Add("ep:logout");
            descriptor.Permissions.Add("ep:userinfo");
            descriptor.Permissions.Add("gt:authorization_code");
            descriptor.Permissions.Add("gt:refresh_token");
            descriptor.Permissions.Add("rst:code");
            descriptor.Permissions.Add("scp:openid");
            descriptor.Permissions.Add("scp:email");
            descriptor.Permissions.Add("scp:profile");
            descriptor.Permissions.Add("scp:roles");
            descriptor.Permissions.Add("scp:offline_access");
            descriptor.RedirectUris.Add(new Uri("http://localhost:5100/signin-oidc"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("http://localhost:5100/"));
            await appManager.UpdateAsync(existing, descriptor);
            return Ok(new { message = "web-ui client updated" });
        }

        var newDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "web-ui",
            ClientSecret = null,
            DisplayName = "Web UI — Portail développeur",
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
        };

        newDescriptor.RedirectUris.Add(new Uri("http://localhost:5100/signin-oidc"));
        newDescriptor.PostLogoutRedirectUris.Add(new Uri("http://localhost:5100/"));
        newDescriptor.Permissions.Add("ep:token");
        newDescriptor.Permissions.Add("ep:authorization");
        newDescriptor.Permissions.Add("ep:logout");
        newDescriptor.Permissions.Add("ep:userinfo");
        newDescriptor.Permissions.Add("gt:authorization_code");
        newDescriptor.Permissions.Add("gt:refresh_token");
        newDescriptor.Permissions.Add("rst:code");
        newDescriptor.Permissions.Add("scp:openid");
        newDescriptor.Permissions.Add("scp:email");
        newDescriptor.Permissions.Add("scp:profile");
        newDescriptor.Permissions.Add("scp:roles");
        newDescriptor.Permissions.Add("scp:offline_access");
        await appManager.CreateAsync(newDescriptor);
        return Ok(new { message = "web-ui client created" });
    }

    /// <summary>Demande de réinitialisation de mot de passe.</summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Ok(new { message = "Si le compte existe, un email de réinitialisation a été envoyé." });

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = $"{_smtp.AppBaseUrl.TrimEnd('/')}/Account/ResetPassword?email={Uri.EscapeDataString(request.Email)}&code={Uri.EscapeDataString(token)}";
        await _emailSender.SendPasswordResetLinkAsync(user, request.Email, resetLink);
        return Ok(new { message = "Si le compte existe, un email de réinitialisation a été envoyé." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return BadRequest(new { message = "Email invalide", code = "INVALID_EMAIL" });

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)), code = "RESET_FAILED" });

        return Ok(new { message = "Mot de passe réinitialisé avec succès." });
    }

    /// <summary>Refresh token — convertit un appel JSON en requête OpenIddict form.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var httpContext = HttpContext;
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = request.RefreshToken ?? string.Empty,
            ["client_id"] = "web-ui",
            ["scope"] = "openid profile email roles offline_access"
        });
        return await TokenExchange();
    }
}

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string Password);
public record RefreshTokenRequest(string? RefreshToken);
