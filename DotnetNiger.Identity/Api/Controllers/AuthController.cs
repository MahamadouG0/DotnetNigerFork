// Controleur API Identity: AuthController
using Asp.Versioning;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
// Endpoints publics pour authentification et reinitialisation.
public class AuthController : ControllerBase
{
    // Endpoints publics pour l'authentification.
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthDto>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthDto>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var token = await _authService.RequestPasswordResetAsync(request);
        if (_environment.IsDevelopment())
        {
            return Ok(new { message = "Reset token generated.", token });
        }

        return Ok(new { message = "If the email exists, a reset link was sent." });
    }

    [HttpPost("request-email-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestEmailVerification([FromBody] RequestEmailVerificationRequest request)
    {
        var token = await _authService.RequestEmailVerificationAsync(request);
        if (_environment.IsDevelopment())
        {
            return Ok(new { message = "Verification token generated.", token });
        }

        return Ok(new { message = "If the email exists, a verification email was sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(new { message = "Password reset successful." });
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        await _authService.VerifyEmailAsync(request);
        return Ok(new { message = "Email verified." });
    }
}
