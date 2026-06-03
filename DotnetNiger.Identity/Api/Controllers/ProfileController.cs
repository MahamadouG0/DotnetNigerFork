using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static OpenIddict.Abstractions.OpenIddictConstants;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Services;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]

[Route("api/v{version:apiVersion}/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuthService _authService;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        AuthService authService,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _authService = authService;
        _signInManager = signInManager;
    }

    /// <summary>Retourne le profil de l'utilisateur connecté.</summary>
    [HttpGet]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserProfileResponse(
            user.Id, user.Email!, user.FirstName, user.LastName, user.AvatarUrl,
            user.TenantId, roles));
    }

    /// <summary>Met à jour le profil de l'utilisateur connecté (prénom, nom, avatar).</summary>
    [HttpPut]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile(
        [FromBody] UpdateUserRequest request)
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserProfileResponse(
            user.Id, user.Email!, user.FirstName, user.LastName, user.AvatarUrl,
            user.TenantId, roles));
    }

    /// <summary>Supprime le compte de l'utilisateur connecté.</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteProfile()
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        await _userManager.DeleteAsync(user);
        return NoContent();
    }

    [HttpGet("two-factor/status")]
    public async Task<ActionResult<TwoFactorStatusResponse>> GetTwoFactorStatus()
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();
        var status = await _authService.GetTwoFactorStatusAsync(Guid.Parse(userId));
        return Ok(status);
    }

    [HttpPost("two-factor/setup")]
    public async Task<ActionResult<TwoFactorSetupResponse>> SetupTwoFactor()
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();
        var (sharedKey, authenticatorUri) = await _authService.GetTwoFactorSetupAsync(Guid.Parse(userId));
        return Ok(new TwoFactorSetupResponse(sharedKey, authenticatorUri, false));
    }

    [HttpPost("two-factor/enable")]
    public async Task<ActionResult> EnableTwoFactor([FromBody] Enable2faRequest request)
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();
        var (success, recoveryCodes) = await _authService.EnableTwoFactorAsync(Guid.Parse(userId), request.Code);
        if (!success) return BadRequest(new ErrorResponse("Impossible d'activer la double authentification"));
        return Ok(new RecoveryCodesResponse(recoveryCodes, recoveryCodes.Length));
    }

    [HttpPost("two-factor/disable")]
    public async Task<ActionResult> DisableTwoFactor([FromBody] Disable2faRequest request)
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, request.Code);
        if (!isValid)
            return BadRequest(new ErrorResponse("Code de vérification invalide"));

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        return Ok(new { message = "Double authentification désactivée" });
    }

    [HttpPost("two-factor/recovery-codes")]
    public async Task<ActionResult<RecoveryCodesResponse>> GenerateRecoveryCodes()
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();
        var codes = await _authService.GenerateRecoveryCodesAsync(Guid.Parse(userId));
        return Ok(new RecoveryCodesResponse(codes, codes.Length));
    }

    [HttpGet("login-history")]
    public async Task<ActionResult> GetLoginHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();
        var result = await _authService.GetLoginHistoryAsync(Guid.Parse(userId), Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
        return Ok(result);
    }

    [HttpPost("change-email")]
    public async Task<ActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();
        await _authService.ChangeEmailAsync(Guid.Parse(userId), request.NewEmail);
        return Ok(new { message = "Un code de confirmation a été envoyé à votre nouvelle adresse email." });
    }

    [HttpPost("confirm-change-email")]
    public async Task<ActionResult> ConfirmChangeEmail([FromBody] ConfirmChangeEmailRequest request)
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();
        await _authService.ConfirmChangeEmailAsync(Guid.Parse(userId), request.Code);
        return Ok(new { message = "Adresse email modifiée avec succès." });
    }
}
