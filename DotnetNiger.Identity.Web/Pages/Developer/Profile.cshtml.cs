using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(IHttpClientFactory http, IConfiguration config, ILogger<ProfileModel> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    public string Email { get; set; } = "";
    public string TenantId { get; set; } = "";
    public List<string> Roles { get; set; } = [];
    public string Message { get; set; } = "";
    public bool IsError { get; set; }

    public bool TwoFactorEnabled { get; set; }
    public int RecoveryCodesLeft { get; set; }
    public string? SharedKey { get; set; }
    public string? AuthenticatorUri { get; set; }
    public string[]? RecoveryCodes { get; set; }

    [BindProperty]
    public TwoFactorInput TwoFactorInput { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadProfileAsync();
        await LoadTwoFactorStatusAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = JsonSerializer.Serialize(new
        {
            firstName = Input.FirstName,
            lastName = Input.LastName,
            avatarUrl = Input.AvatarUrl
        });

        var response = await client.PutAsync(
            $"{identityUrl}/api/v1/profile",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            Message = "Profil mis à jour avec succès.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadProfileAsync();
        await LoadTwoFactorStatusAsync();
        return Page();
    }

    private async Task LoadProfileAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync($"{identityUrl}/api/v1/profile");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<ProfileResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (profile != null)
                {
                    Input.FirstName = profile.FirstName ?? "";
                    Input.LastName = profile.LastName ?? "";
                    Input.AvatarUrl = profile.AvatarUrl ?? "";
                    Email = profile.Email;
                    TenantId = profile.TenantId?.ToString() ?? "";
                    Roles = profile.Roles?.ToList() ?? [];
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load profile from Identity API, falling back to claims");
            Input.FirstName = User.FindFirst("given_name")?.Value ?? User.FindFirst(ClaimTypes.GivenName)?.Value ?? "";
            Input.LastName = User.FindFirst("family_name")?.Value ?? User.FindFirst(ClaimTypes.Surname)?.Value ?? "";
            Email = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            TenantId = User.FindFirst("tenant_id")?.Value ?? "";
            Roles = User.FindAll("role").Select(c => c.Value).ToList();
        }
    }

    private async Task LoadTwoFactorStatusAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync($"{identityUrl}/api/v1/profile/two-factor/status");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                TwoFactorEnabled = root.GetProperty("twoFactorEnabled").GetBoolean();
                RecoveryCodesLeft = root.GetProperty("recoveryCodesLeft").GetInt32();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load 2FA status from Identity API");
        }
    }

    [BindProperty]
    public ChangePasswordInput PasswordInput { get; set; } = new();

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = JsonSerializer.Serialize(new
        {
            currentPassword = PasswordInput.CurrentPassword,
            newPassword = PasswordInput.NewPassword
        });

        var response = await client.PostAsync(
            $"{identityUrl}/api/account/change-password",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            Message = "Mot de passe changé avec succès.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        return Page();
    }

    public async Task<IActionResult> OnGetTwoFactorStatusAsync()
    {
        await LoadTwoFactorStatusAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSetupTwoFactorAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync($"{identityUrl}/api/v1/profile/two-factor/setup", null);

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            SharedKey = root.GetProperty("sharedKey").GetString();
            AuthenticatorUri = root.GetProperty("authenticatorUri").GetString();
            Message = "Scannez le code QR avec votre application d'authentification.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadTwoFactorStatusAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostEnableTwoFactorAsync()
    {
        if (string.IsNullOrEmpty(TwoFactorInput.Code))
        {
            Message = "Veuillez entrer le code de vérification.";
            IsError = true;
            return Page();
        }

        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = JsonSerializer.Serialize(new { code = TwoFactorInput.Code });
        var response = await client.PostAsync(
            $"{identityUrl}/api/v1/profile/two-factor/enable",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("recoveryCodes", out var codes))
            {
                RecoveryCodes = codes.EnumerateArray().Select(c => c.GetString()!).ToArray();
            }
            Message = "Double authentification activée avec succès.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadTwoFactorStatusAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDisableTwoFactorAsync()
    {
        if (string.IsNullOrEmpty(TwoFactorInput.Code))
        {
            Message = "Veuillez entrer le code de vérification.";
            IsError = true;
            return Page();
        }

        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = JsonSerializer.Serialize(new { code = TwoFactorInput.Code });
        var response = await client.PostAsync(
            $"{identityUrl}/api/v1/profile/two-factor/disable",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            SharedKey = null;
            AuthenticatorUri = null;
            RecoveryCodes = null;
            Message = "Double authentification désactivée.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadTwoFactorStatusAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostGenerateRecoveryCodesAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync($"{identityUrl}/api/v1/profile/two-factor/recovery-codes", null);

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("recoveryCodes", out var codes))
            {
                RecoveryCodes = codes.EnumerateArray().Select(c => c.GetString()!).ToArray();
            }
            Message = "Codes de récupération générés. Sauvegardez-les dans un endroit sûr.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadTwoFactorStatusAsync();
        return Page();
    }

    public class ChangePasswordInput
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}

public class ProfileInput
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
}

public class TwoFactorInput
{
    public string Code { get; set; } = "";
    public string SharedKey { get; set; } = "";
    public string AuthenticatorUri { get; set; } = "";
}

public class ProfileResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? TenantId { get; set; }
    public List<string>? Roles { get; set; }
}
