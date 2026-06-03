using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer.Admin;

[Authorize(Roles = "Admin")]
public class RolesModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public RolesModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    [BindProperty(SupportsGet = true)]
    public Guid TenantId { get; set; }

    public List<RoleItem> Roles { get; set; } = [];
    public List<PermissionGroup> PermissionGroups { get; set; } = [];
    public HashSet<Guid> RolePermissionIds { get; set; } = [];
    public string Message { get; set; } = "";
    public bool IsError { get; set; }

    [BindProperty]
    public CreateRoleInput CreateInput { get; set; } = new();

    [BindProperty]
    public EditRoleInput EditInput { get; set; } = new();

    [BindProperty]
    public List<Guid> SelectedPermissionIds { get; set; } = [];

    public List<UserItem> AllUsers { get; set; } = [];

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            return Page();
        }

        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var body = JsonSerializer.Serialize(new
        {
            name = CreateInput.Name,
            description = CreateInput.Description ?? "",
            tenantId = TenantId
        });

        var response = await client.PostAsync(
            $"{identityUrl}/api/v1/{TenantId}/roles",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            Message = "Rôle créé.";
            IsError = false;
            CreateInput = new CreateRoleInput();
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAssignPermissionsAsync(Guid roleId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var body = JsonSerializer.Serialize(new
        {
            roleId,
            permissionIds = SelectedPermissionIds
        });

        var response = await client.PostAsync(
            $"{identityUrl}/api/v1/{TenantId}/permissions/assign",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            Message = "Permissions mises à jour.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid roleId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var response = await client.DeleteAsync($"{identityUrl}/api/v1/{TenantId}/roles/{roleId}");
        if (response.IsSuccessStatusCode)
        {
            Message = "Rôle supprimé.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(Guid roleId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var body = JsonSerializer.Serialize(new
        {
            name = EditInput.Name,
            description = EditInput.Description ?? ""
        });

        var response = await client.PutAsync(
            $"{identityUrl}/api/v1/{TenantId}/roles/{roleId}",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            Message = "Rôle mis à jour.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddUserToRoleAsync(Guid roleId, Guid userId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var response = await client.PostAsync(
            $"{identityUrl}/api/v1/{TenantId}/roles/{roleId}/users/{userId}",
            null);

        if (response.IsSuccessStatusCode)
        {
            Message = "Utilisateur ajouté au rôle.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveUserFromRoleAsync(Guid roleId, Guid userId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        var response = await client.DeleteAsync(
            $"{identityUrl}/api/v1/{TenantId}/roles/{roleId}/users/{userId}");

        if (response.IsSuccessStatusCode)
        {
            Message = "Utilisateur retiré du rôle.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadDataAsync();
        return Page();
    }

    private async Task LoadDataAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = await CreateClientAsync(identityUrl);

        try
        {
            var rolesResp = await client.GetAsync($"{identityUrl}/api/v1/{TenantId}/roles");
            if (rolesResp.IsSuccessStatusCode)
            {
                var json = await rolesResp.Content.ReadAsStringAsync();
                Roles = JsonSerializer.Deserialize<List<RoleItem>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
        }
        catch { Roles = []; }

        try
        {
            var permResp = await client.GetAsync($"{identityUrl}/api/v1/{TenantId}/permissions/grouped");
            if (permResp.IsSuccessStatusCode)
            {
                var json = await permResp.Content.ReadAsStringAsync();
                PermissionGroups = JsonSerializer.Deserialize<List<PermissionGroup>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
        }
        catch { PermissionGroups = []; }

        try
        {
            var usersResp = await client.GetAsync($"{identityUrl}/api/v1/{TenantId}/users");
            if (usersResp.IsSuccessStatusCode)
            {
                var json = await usersResp.Content.ReadAsStringAsync();
                AllUsers = JsonSerializer.Deserialize<List<UserItem>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
        }
        catch { AllUsers = []; }
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

public class RoleItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int UserCount { get; set; }
}

public class PermissionGroup
{
    public string Category { get; set; } = "";
    public List<PermissionItem> Permissions { get; set; } = [];
}

public class PermissionItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
}

public class CreateRoleInput
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class EditRoleInput
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
