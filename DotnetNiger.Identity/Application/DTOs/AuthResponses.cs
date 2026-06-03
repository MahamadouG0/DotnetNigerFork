namespace DotnetNiger.Identity.Application.DTOs;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    Guid UserId,
    string Email,
    Guid? TenantId,
    IList<string> Roles);

public record UserInfoResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    Guid? TenantId,
    bool IsActive,
    IList<string> Roles,
    IList<string> Permissions,
    bool RememberMe = false);

public record RegisterTenantResponse(
    Guid TenantId,
    string TenantName,
    string TenantSlug,
    string AdminEmail,
    string ClientId,
    string ClientSecret,
    Guid ApiKeyId,
    string ApiKeySecret);

public record TwoFactorStatusResponse(
    bool IsEnabled,
    bool IsMachineRemembered,
    int RecoveryCodesLeft);

public record TwoFactorSetupResponse(
    string SharedKey,
    string AuthenticatorUri,
    bool IsEnabled);

public record TwoFactorRequiredResponse(
    bool RequiresTwoFactor,
    string ChallengeToken,
    string? TwoFactorType = "authenticator");

public record RecoveryCodesResponse(
    IList<string> RecoveryCodes,
    int RemainingCount);

public record TwoFactorChallenge(
    Guid UserId,
    string Email,
    Guid TenantId,
    DateTime ExpiresAt);

public record ConsentResponse(
    string ConsentType,
    string ConsentVersion,
    bool Granted,
    DateTime CreatedAt);

public record DataExportResponse(
    byte[] ZipData,
    string FileName);

public record ForgetMeResponse(
    string Message,
    DateTime CompletedAt);
