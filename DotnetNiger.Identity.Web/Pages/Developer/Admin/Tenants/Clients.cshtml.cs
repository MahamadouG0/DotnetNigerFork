using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer.Admin;

[Authorize(Roles = "Admin")]
public class ClientsModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public ClientsModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    [BindProperty(SupportsGet = true)]
    public Guid TenantId { get; set; }

    public List<ClientItem> Clients { get; set; } = [];
    public string Message { get; set; } = "";
    public bool IsError { get; set; }

    [BindProperty]
    public CreateClientInput Input { get; set; } = new();

    [BindProperty]
    public EditClientInput EditInput { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadClientsAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        try
        {
            var body = JsonSerializer.Serialize(new
            {
                clientName = Input.ClientName,
                description = Input.Description,
                allowedGrantTypes = Input.AllowedGrantTypes?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? new[] { "authorization_code" },
                redirectUris = new[] { Input.RedirectUri }.Where(u => !string.IsNullOrEmpty(u)).ToArray(),
                postLogoutRedirectUris = new[] { Input.PostLogoutRedirectUri }.Where(u => !string.IsNullOrEmpty(u)).ToArray()
            });
            var resp = await client.PostAsync($"{identityUrl}/api/v1/admin/tenants/{TenantId}/clients",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (resp.IsSuccessStatusCode)
            {
                Message = "Client créé avec succès.";
                IsError = false;
                Input = new();
            }
            else
            {
                var err = await resp.Content.ReadAsStringAsync();
                Message = $"Erreur : {err}";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            Message = $"Erreur : {ex.Message}";
            IsError = true;
        }

        await LoadClientsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid clientId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        try
        {
            var resp = await client.DeleteAsync($"{identityUrl}/api/v1/admin/tenants/{TenantId}/clients/{clientId}");
            if (resp.IsSuccessStatusCode)
            {
                Message = "Client supprimé avec succès.";
                IsError = false;
            }
            else
            {
                var err = await resp.Content.ReadAsStringAsync();
                Message = $"Erreur : {err}";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            Message = $"Erreur : {ex.Message}";
            IsError = true;
        }

        await LoadClientsAsync();
        return Page();
    }

    private async Task LoadClientsAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        try
        {
            var resp = await client.GetAsync($"{identityUrl}/api/v1/admin/tenants/{TenantId}/clients");
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                Clients = JsonSerializer.Deserialize<List<ClientItem>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
        }
        catch { Clients = []; }
    }

    public async Task<IActionResult> OnPostEditAsync(Guid clientId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var body = JsonSerializer.Serialize(new
        {
            clientName = EditInput.ClientName,
            description = EditInput.Description,
            allowedGrantTypes = EditInput.AllowedGrantTypes?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? new[] { "authorization_code" },
            redirectUris = new[] { EditInput.RedirectUri }.Where(u => !string.IsNullOrEmpty(u)).ToArray(),
            postLogoutRedirectUris = new[] { EditInput.PostLogoutRedirectUri }.Where(u => !string.IsNullOrEmpty(u)).ToArray()
        });

        try
        {
            var resp = await client.PutAsync($"{identityUrl}/api/v1/admin/tenants/{TenantId}/clients/{clientId}",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (resp.IsSuccessStatusCode)
            {
                Message = "Client modifié avec succès.";
                IsError = false;
            }
            else
            {
                var err = await resp.Content.ReadAsStringAsync();
                Message = $"Erreur : {err}";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            Message = $"Erreur : {ex.Message}";
            IsError = true;
        }

        await LoadClientsAsync();
        return Page();
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

public class ClientItem
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string? Description { get; set; }
    public List<string> AllowedGrantTypes { get; set; } = [];
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateClientInput
{
    public string ClientName { get; set; } = "";
    public string? Description { get; set; }
    public string? AllowedGrantTypes { get; set; }
    public string? RedirectUri { get; set; }
    public string? PostLogoutRedirectUri { get; set; }
}

public class EditClientInput
{
    public string? ClientName { get; set; }
    public string? Description { get; set; }
    public string? AllowedGrantTypes { get; set; }
    public string? RedirectUri { get; set; }
    public string? PostLogoutRedirectUri { get; set; }
}
