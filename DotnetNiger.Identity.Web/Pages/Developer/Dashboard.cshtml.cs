using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public DashboardModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public DashboardStats Stats { get; set; } = new();

    public async Task OnGetAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var profileResp = await client.GetAsync($"{identityUrl}/api/v1/profile");
            if (profileResp.IsSuccessStatusCode)
            {
                var profile = JsonSerializer.Deserialize<ProfileJson>(
                    await profileResp.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (profile != null)
                {
                    Stats.TenantName = profile.TenantId?.ToString() ?? "—";
                    Stats.TenantId = profile.TenantId?.ToString() ?? "";
                }
            }
        }
        catch { Stats = new DashboardStats(); }

        try
        {
            var tenantId = User.FindFirst("tenant_id")?.Value ?? "";
            var keysResp = await client.GetAsync($"{identityUrl}/api/v1/admin/tenants/{tenantId}/api-keys");
            if (keysResp.IsSuccessStatusCode)
            {
                var keys = JsonSerializer.Deserialize<List<object>>(
                    await keysResp.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Stats.ActiveApiKeys = keys?.Count ?? 0;
            }
        }
        catch { Stats = new DashboardStats(); }

        try
        {
            var svcResp = await client.GetAsync($"{identityUrl}/api/v1/external-services");
            if (svcResp.IsSuccessStatusCode)
            {
                var svcs = JsonSerializer.Deserialize<List<object>>(
                    await svcResp.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Stats.ActiveServices = svcs?.Count ?? 0;
            }
        }
        catch { Stats = new DashboardStats(); }
    }
}

public class DashboardStats
{
    public string TenantName { get; set; } = "—";
    public string TenantId { get; set; } = "";
    public int ActiveApiKeys { get; set; }
    public int ActiveServices { get; set; }
    public bool GatewayConnected { get; set; }
}

public class ProfileJson
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid? TenantId { get; set; }
    public List<string>? Roles { get; set; }
}
