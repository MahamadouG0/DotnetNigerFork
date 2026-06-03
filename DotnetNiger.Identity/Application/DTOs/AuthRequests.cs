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

public record RegisterTenantRequest(
    [Required][StringLength(100, MinimumLength = 2)] string CompanyName,
    [Required][StringLength(50, MinimumLength = 2)][RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Le slug doit contenir uniquement des lettres minuscules, chiffres et tirets")] string Slug,
    [Required][EmailAddress] string AdminEmail,
    [Required][StringLength(100, MinimumLength = 8)] string AdminPassword,
    [Required][StringLength(50, MinimumLength = 1)] string AdminFirstName,
    [Required][StringLength(50, MinimumLength = 1)] string AdminLastName);

public record Enable2faRequest(
    [Required] string Code);

public record Verify2faRequest(
    [Required] string Code,
    [Required] string ChallengeToken,
    bool RememberMachine = false);

public record Disable2faRequest(
    [Required] string Code);

public record TwoFactorRecoveryCodeRequest(
    [Required] string RecoveryCode,
    [Required] string ChallengeToken);

public record ConsentRequest(
    [Required] string ConsentType,
    [Required] string ConsentVersion,
    bool Granted = true);

public record ChangeEmailRequest(
    [Required][EmailAddress] string NewEmail);

public record ConfirmChangeEmailRequest(
    [Required][EmailAddress] string NewEmail,
    [Required] string Code);

public record InviteAdminRequest(
    [Required][EmailAddress] string Email,
    [Required] string Role);
