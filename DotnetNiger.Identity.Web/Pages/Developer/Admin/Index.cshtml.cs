using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public IndexModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public AdminStats Stats { get; set; } = new();

    public async Task OnGetAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var resp = await client.GetAsync($"{identityUrl}/api/v1/admin/stats");
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                var s = JsonSerializer.Deserialize<AdminStatsJson>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (s != null)
                {
                    Stats.TenantCount = s.TenantCount;
                    Stats.UserCount = s.UserCount;
                    Stats.RoleCount = s.RoleCount;
                    Stats.PermissionCount = s.PermissionCount;
                    Stats.ApiKeyCount = s.ApiKeyCount;
                    Stats.ServiceCount = s.ServiceCount;
                    Stats.ClientCount = s.ClientCount;
                }
            }
        }
        catch { Stats = new AdminStats(); }
    }
}

public class AdminStats
{
    public int TenantCount { get; set; }
    public int UserCount { get; set; }
    public int RoleCount { get; set; }
    public int PermissionCount { get; set; }
    public int ApiKeyCount { get; set; }
    public int ServiceCount { get; set; }
    public int ClientCount { get; set; }
}

public class AdminStatsJson
{
    public int TenantCount { get; set; }
    public int UserCount { get; set; }
    public int RoleCount { get; set; }
    public int PermissionCount { get; set; }
    public int ApiKeyCount { get; set; }
    public int ServiceCount { get; set; }
    public int ClientCount { get; set; }
}
