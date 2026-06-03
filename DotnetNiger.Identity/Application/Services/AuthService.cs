using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.DTOs;

namespace DotnetNiger.Identity.Application.Services;

public class AuthService
{
    private static readonly char[] CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TenantContext _tenantContext;
    private readonly IdentityDbContext _db;
    private readonly IEmailSender<ApplicationUser> _emailSender;
    private readonly SmtpOptions _smtp;

    public AuthService(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TenantContext tenantContext, IdentityDbContext db,
        IEmailSender<ApplicationUser> emailSender,
        IOptions<SmtpOptions> smtp)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tenantContext = tenantContext;
        _db = db;
        _emailSender = emailSender;
        _smtp = smtp.Value;
    }

    public async Task<(ApplicationUser user, IList<string> roles)> ValidateCredentialsAsync(
        string email, string password, Guid? tenantId = null)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Email ou mot de passe incorrect");

        if (tenantId.HasValue && user.TenantId != tenantId.Value)
            throw new UnauthorizedAccessException("Utilisateur non trouvé dans ce tenant");

        if (!await _userManager.IsEmailConfirmedAsync(user))
            throw new UnauthorizedAccessException("Email non confirmé");

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, true);
        if (result.IsLockedOut)
            throw new UnauthorizedAccessException("Compte temporairement verrouillé");
        if (!result.Succeeded)
            throw new UnauthorizedAccessException("Email ou mot de passe incorrect");

        var roles = await _userManager.GetRolesAsync(user);
        _tenantContext.TenantId = user.TenantId;
        return (user, roles);
    }

    public async Task<(ApplicationUser user, string code)> RegisterAsync(string email, string password,
        string firstName, string lastName, Guid? tenantId = null)
    {
        if (await _userManager.FindByEmailAsync(email) != null)
            throw new InvalidOperationException("Un compte avec cet email existe déjà");

        var tenant = tenantId.HasValue
            ? await _db.Tenants.FindAsync(tenantId.Value)
            : await _db.Tenants.FirstOrDefaultAsync();

        if (tenant == null)
            throw new InvalidOperationException("Aucun tenant trouvé");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            TenantId = tenant.Id,
            IsActive = true,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Erreur création: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await _userManager.AddToRoleAsync(user, "User");

        var code = GenerateCode();
        user.EmailConfirmationCode = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
        await _userManager.UpdateAsync(user);

        await SendConfirmationEmailAsync(user, code);

        _tenantContext.TenantId = user.TenantId;
        return (user, code);
    }

    public async Task ConfirmEmailAsync(string email, string code)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            throw new InvalidOperationException("Utilisateur non trouvé");

        if (user.EmailConfirmed)
            throw new InvalidOperationException("Email déjà confirmé");

        if (user.EmailConfirmationCode == null || user.EmailConfirmationCodeExpiry == null)
            throw new InvalidOperationException("Aucun code de confirmation trouvé");

        if (user.EmailConfirmationCodeExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Code de confirmation expiré");

        var hashedCode = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        if (!string.Equals(user.EmailConfirmationCode, hashedCode, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Code de confirmation invalide");

        user.EmailConfirmed = true;
        user.EmailConfirmationCode = null;
        user.EmailConfirmationCodeExpiry = null;
        await _userManager.UpdateAsync(user);
    }

    public async Task ResendConfirmationCodeAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            throw new InvalidOperationException("Utilisateur non trouvé");

        if (user.EmailConfirmed)
            throw new InvalidOperationException("Email déjà confirmé");

        var code = GenerateCode();
        user.EmailConfirmationCode = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
        await _userManager.UpdateAsync(user);

        await SendConfirmationEmailAsync(user, code);
    }

    public async Task<(ApplicationUser user, IList<string> roles)> HandleExternalLoginAsync(string provider)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            throw new InvalidOperationException("Erreur lors du login externe");

        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false);
        if (result.Succeeded)
        {
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            var roles = await _userManager.GetRolesAsync(user!);
            _tenantContext.TenantId = user!.TenantId;
            return (user, roles)!;
        }

        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
            throw new InvalidOperationException("Email requis pour le login externe");

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            await _userManager.AddLoginAsync(existingUser, info);
            existingUser.EmailConfirmed = true;
            await _userManager.UpdateAsync(existingUser);
            var roles = await _userManager.GetRolesAsync(existingUser);
            _tenantContext.TenantId = existingUser.TenantId;
            return (existingUser, roles);
        }

        var tenant = await _db.Tenants.FirstOrDefaultAsync();
        if (tenant == null)
            throw new InvalidOperationException("Aucun tenant configuré");

        var newUser = new ApplicationUser
        {
            UserName = email, Email = email, EmailConfirmed = true,
            TenantId = tenant.Id,
            FirstName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value,
            LastName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value
        };
        var createResult = await _userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
            throw new InvalidOperationException("Erreur création utilisateur");

        await _userManager.AddLoginAsync(newUser, info);
        await _userManager.AddToRoleAsync(newUser, "User");
        _tenantContext.TenantId = newUser.TenantId;
        return (newUser, new List<string> { "User" });
    }

    private static string GenerateCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(6);
        var code = new char[6];
        for (int i = 0; i < 6; i++)
            code[i] = CodeChars[bytes[i] % CodeChars.Length];
        return new string(code);
    }

    public async Task<bool> RequiresTwoFactorAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;
        return user.TwoFactorEnabled;
    }

    public async Task<(string sharedKey, string authenticatorUri)> GetTwoFactorSetupAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new InvalidOperationException("User not found");

        await _userManager.ResetAuthenticatorKeyAsync(user);
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var sharedKey = string.Join(' ', unformattedKey!.Chunk(4).Select(c => new string(c)));

        var email = await _userManager.GetEmailAsync(user);
        var appName = "DotnetNiger";
        var authenticatorUri = $"otpauth://totp/{appName}:{Uri.EscapeDataString(email!)}?secret={unformattedKey}&issuer={appName}&digits=6";

        return (sharedKey, authenticatorUri);
    }

    public async Task<(bool success, string[] recoveryCodes)> EnableTwoFactorAsync(Guid userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new InvalidOperationException("User not found");

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!isValid)
            throw new InvalidOperationException("Code de vérification invalide");

        var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!result.Succeeded)
            throw new InvalidOperationException("Impossible d'activer la double authentification");

        if ((await _userManager.CountRecoveryCodesAsync(user)) == 0)
        {
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            return (true, recoveryCodes?.ToArray() ?? []);
        }

        return (true, []);
    }

    public async Task<bool> VerifyTwoFactorCodeAsync(Guid userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        return await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);
    }

    public async Task<TwoFactorStatusResponse> GetTwoFactorStatusAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new InvalidOperationException("User not found");

        var recoveryCodes = await _userManager.CountRecoveryCodesAsync(user);
        return new TwoFactorStatusResponse(user.TwoFactorEnabled, false, recoveryCodes);
    }

    public async Task<string[]> GenerateRecoveryCodesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new InvalidOperationException("User not found");

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return codes?.ToArray() ?? [];
    }

    public async Task ChangeEmailAsync(Guid userId, string newEmail)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new InvalidOperationException("Utilisateur non trouvé");

        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Le nouvel email est identique à l'email actuel");

        var existing = await _userManager.FindByEmailAsync(newEmail);
        if (existing != null && existing.Id != userId)
            throw new InvalidOperationException("Cet email est déjà utilisé par un autre compte");

        var code = GenerateCode();
        user.EmailConfirmationCode = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(15);

        // Store the pending new email (we'll need a field for this)
        user.PendingEmail = newEmail;
        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrEmpty(_smtp.Host))
        {
            var confirmUrl = $"{_smtp.AppBaseUrl}/api/v1/profile/confirm-change-email?email={Uri.EscapeDataString(newEmail)}&code={Uri.EscapeDataString(code)}";
            if (_emailSender is EmailSender typed)
                await typed.SendConfirmationCodeAsync(user, user.Email!, code, confirmUrl);
        }
    }

    public async Task ConfirmChangeEmailAsync(Guid userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new InvalidOperationException("Utilisateur non trouvé");

        if (user.PendingEmail == null)
            throw new InvalidOperationException("Aucun changement d'email en attente");

        if (user.EmailConfirmationCode == null || user.EmailConfirmationCodeExpiry == null)
            throw new InvalidOperationException("Aucun code de confirmation trouvé");

        if (user.EmailConfirmationCodeExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Code de confirmation expiré");

        var hashedCode = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        if (!string.Equals(user.EmailConfirmationCode, hashedCode, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Code de confirmation invalide");

        var newEmail = user.PendingEmail;
        user.Email = newEmail;
        user.UserName = newEmail;
        user.NormalizedEmail = _userManager.NormalizeEmail(newEmail);
        user.NormalizedUserName = _userManager.NormalizeName(newEmail);
        user.PendingEmail = null;
        user.EmailConfirmationCode = null;
        user.EmailConfirmationCodeExpiry = null;
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
    }

    public async Task RecordLoginAsync(Guid userId, string ipAddress, string userAgent, bool success, string? provider = null, string? failureReason = null)
    {
        var entry = new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Provider = provider,
            Success = success,
            FailureReason = failureReason
        };
        _db.LoginHistories.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<object> GetLoginHistoryAsync(Guid userId, int page, int pageSize)
    {
        var query = _db.LoginHistories.Where(h => h.UserId == userId);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new
            {
                h.Id,
                h.IpAddress,
                h.UserAgent,
                h.Provider,
                h.Success,
                h.FailureReason,
                h.CreatedAt
            })
            .ToListAsync();

        return new { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user, string code)
    {
        if (!string.IsNullOrEmpty(_smtp.Host))
        {
            var confirmUrl = $"{_smtp.AppBaseUrl}/api/v1/auth/confirm-email?email={Uri.EscapeDataString(user.Email!)}&code={Uri.EscapeDataString(code)}";
            await _emailSender.SendConfirmationLinkAsync(user, user.Email!, confirmUrl);

            if (_emailSender is EmailSender typed)
                await typed.SendConfirmationCodeAsync(user, user.Email!, code, confirmUrl);
        }
    }
}
