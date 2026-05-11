using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs;

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password,
    Guid? TenantId = null,
    bool RememberMe = false);

public record RefreshTokenRequest(
    [Required] string RefreshToken);

public record ExternalLoginRequest(
    [Required] string Provider,
    string? ReturnUrl);

public record ForgotPasswordRequest(
    [Required][EmailAddress] string Email);

public record ResetPasswordRequest(
    [Required][EmailAddress] string Email,
    [Required] string Token,
    [Required] string NewPassword,
    Guid? TenantId = null);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required] string NewPassword);

public record RegisterRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password,
    [Required] string FirstName,
    [Required] string LastName,
    Guid? TenantId = null);

public record ConfirmEmailRequest(
    [Required][EmailAddress] string Email,
    [Required] string Code);
