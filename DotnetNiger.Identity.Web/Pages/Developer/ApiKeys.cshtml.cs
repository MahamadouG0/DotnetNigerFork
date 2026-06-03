using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer;

[Authorize]
public class ApiKeysModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public ApiKeysModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public List<ApiKeyItem> ApiKeys { get; set; } = [];
    public string Message { get; set; } = "";
    public bool IsError { get; set; }

    [BindProperty]
    public string NewKeyName { get; set; } = "";

    public async Task OnGetAsync()
    {
        await LoadKeysAsync();
    }

    private string GetTenantId()
    {
        return User.FindFirst("tenant_id")?.Value ?? "";
    }

    private string GetApiKeysBaseUrl(string? identityUrl)
    {
        _ = identityUrl ?? throw new InvalidOperationException("Identity:BaseUrl n'est pas configuré.");
        var tenantId = GetTenantId();
        return $"{identityUrl}/api/v1/admin/tenants/{tenantId}/api-keys";
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewKeyName))
        {
            Message = "Le nom de la clé est requis.";
            IsError = true;
            await LoadKeysAsync();
            return Page();
        }

        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = JsonSerializer.Serialize(new { name = NewKeyName });
        var response = await client.PostAsync(
            $"{GetApiKeysBaseUrl(identityUrl)}",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var created = JsonSerializer.Deserialize<ApiKeyCreatedResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (created != null)
                Message = $"Clé créée : {created.Key}. Copiez-la maintenant, elle ne sera plus affichée.";
            else
                Message = "Clé créée avec succès.";
            IsError = false;
            NewKeyName = "";
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadKeysAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid keyId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync($"{GetApiKeysBaseUrl(identityUrl)}/{keyId}");
        if (response.IsSuccessStatusCode)
        {
            Message = "Clé supprimée.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadKeysAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRotateAsync(Guid keyId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync($"{GetApiKeysBaseUrl(identityUrl)}/{keyId}/rotate", null);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var rotated = JsonSerializer.Deserialize<ApiKeyCreatedResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Message = rotated != null
                ? $"Nouvelle clé : {rotated.Key}. Copiez-la maintenant."
                : "Clé rotée avec succès.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadKeysAsync();
        return Page();
    }

    private async Task LoadKeysAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync($"{GetApiKeysBaseUrl(identityUrl)}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                ApiKeys = JsonSerializer.Deserialize<List<ApiKeyItem>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
        }
        catch { ApiKeys = []; }
    }
}

public class ApiKeyItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string KeyPrefix { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ApiKeyCreatedResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Key { get; set; } = "";
    public string KeyPrefix { get; set; } = "";
}
