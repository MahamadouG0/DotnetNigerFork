using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer.Admin;

[Authorize(Roles = "Admin")]
public class TenantsModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public TenantsModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public List<TenantItem> Tenants { get; set; } = [];
    public string Message { get; set; } = "";
    public bool IsError { get; set; }

    [BindProperty]
    public CreateTenantInput CreateInput { get; set; } = new();

    [BindProperty]
    public EditTenantInput EditInput { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadTenantsAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadTenantsAsync();
            return Page();
        }

        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var body = JsonSerializer.Serialize(new
        {
            name = CreateInput.Name,
            slug = CreateInput.Slug,
            description = CreateInput.Description ?? ""
        });

        var response = await client.PostAsync(
            $"{identityUrl}/api/v1/admin/tenants",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            Message = "Tenant créé avec succès !";
            IsError = false;
            CreateInput = new CreateTenantInput();
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadTenantsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(Guid tenantId)
    {
        if (!ModelState.IsValid)
        {
            await LoadTenantsAsync();
            return Page();
        }

        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var body = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(EditInput.Name)) body["name"] = EditInput.Name;
        if (EditInput.Description != null) body["description"] = EditInput.Description;

        var request = new HttpRequestMessage(HttpMethod.Put, $"{identityUrl}/api/v1/admin/tenants/{tenantId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            Message = "Tenant mis à jour.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadTenantsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid tenantId, bool isActive)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var body = JsonSerializer.Serialize(new { isActive = !isActive });
        var request = new HttpRequestMessage(HttpMethod.Put, $"{identityUrl}/api/v1/admin/tenants/{tenantId}")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        await client.SendAsync(request);
        Message = isActive ? "Tenant désactivé." : "Tenant activé.";

        await LoadTenantsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid tenantId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var response = await client.DeleteAsync($"{identityUrl}/api/v1/admin/tenants/{tenantId}");
        if (response.IsSuccessStatusCode)
        {
            Message = "Tenant supprimé.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadTenantsAsync();
        return Page();
    }

    private async Task LoadTenantsAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        try
        {
            var resp = await client.GetAsync($"{identityUrl}/api/v1/admin/tenants");
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                Tenants = JsonSerializer.Deserialize<List<TenantItem>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
        }
        catch { Tenants = []; Message = "Erreur lors du chargement des tenants."; IsError = true; }
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

public class TenantItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string AdminEmail => $"admin@{Slug}.dotnetniger.com";
}

public class CreateTenantInput
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
}

public class EditTenantInput
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
