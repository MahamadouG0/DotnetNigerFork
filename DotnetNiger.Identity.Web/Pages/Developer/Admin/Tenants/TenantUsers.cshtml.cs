using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer.Admin;

[Authorize(Roles = "Admin")]
public class TenantUsersModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public TenantUsersModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    [BindProperty(SupportsGet = true)]
    public Guid TenantId { get; set; }

    public List<UserItem> Users { get; set; } = [];
    public string Message { get; set; } = "";
    public bool IsError { get; set; }

    [BindProperty]
    public CreateUserInput CreateInput { get; set; } = new();

    [BindProperty]
    public EditUserInput EditInput { get; set; } = new();

    [BindProperty]
    public ChangePasswordInput PasswordInput { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadUsersAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUsersAsync();
            return Page();
        }

        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var body = JsonSerializer.Serialize(new
        {
            email = CreateInput.Email,
            password = CreateInput.Password,
            firstName = CreateInput.FirstName ?? "",
            lastName = CreateInput.LastName ?? "",
            tenantId = TenantId,
            roles = CreateInput.Role ? new[] { "User" } : Array.Empty<string>()
        });

        var response = await client.PostAsync(
            $"{identityUrl}/api/v1/{TenantId}/users",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            Message = "Utilisateur créé.";
            IsError = false;
            CreateInput = new CreateUserInput();
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(Guid userId)
    {
        if (!ModelState.IsValid)
        {
            await LoadUsersAsync();
            return Page();
        }

        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var body = JsonSerializer.Serialize(new
        {
            firstName = EditInput.FirstName ?? "",
            lastName = EditInput.LastName ?? ""
        });

        var response = await client.PutAsync(
            $"{identityUrl}/api/v1/{TenantId}/users/{userId}",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            Message = "Utilisateur mis à jour.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(Guid userId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var body = JsonSerializer.Serialize(new
        {
            currentPassword = PasswordInput.CurrentPassword,
            newPassword = PasswordInput.NewPassword
        });

        var response = await client.PostAsync(
            $"{identityUrl}/api/v1/{TenantId}/users/{userId}/change-password",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            Message = "Mot de passe changé.";
            IsError = false;
            PasswordInput = new ChangePasswordInput();
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid userId, bool isActive)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var body = JsonSerializer.Serialize(new { isActive = !isActive });
        var response = await client.PutAsync(
            $"{identityUrl}/api/v1/{TenantId}/users/{userId}",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Message = isActive ? "Utilisateur désactivé." : "Utilisateur activé.";

        await LoadUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid userId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var response = await client.DeleteAsync($"{identityUrl}/api/v1/{TenantId}/users/{userId}");
        if (response.IsSuccessStatusCode)
        {
            Message = "Utilisateur supprimé.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostForgotPasswordAsync(Guid userId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var user = Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            Message = "Utilisateur introuvable.";
            IsError = true;
            await LoadUsersAsync();
            return Page();
        }

        var body = JsonSerializer.Serialize(new { email = user.Email });

        try
        {
            var response = await client.PostAsync(
                $"{identityUrl}/api/v1/{TenantId}/users/forgot-password",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                Message = "Email de réinitialisation envoyé.";
                IsError = false;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Message = $"Erreur : {error}";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            Message = $"Erreur : {ex.Message}";
            IsError = true;
        }

        await LoadUsersAsync();
        return Page();
    }

    private async Task LoadUsersAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        try
        {
            var resp = await client.GetAsync($"{identityUrl}/api/v1/{TenantId}/users");
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                Users = JsonSerializer.Deserialize<List<UserItem>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
        }
        catch (Exception ex) { Message = $"Erreur lors du chargement : {ex.Message}"; IsError = true; }
    }

    private async Task<HttpClient> CreateClientAsync(string? identityUrl)
    {
        _ = identityUrl ?? throw new InvalidOperationException("Identity:BaseUrl n'est pas configuré.");
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public class UserItem
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class CreateUserInput
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Role { get; set; } = true;
}

public class EditUserInput
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class ChangePasswordInput
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
