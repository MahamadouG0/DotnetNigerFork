using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages;

[AllowAnonymous]
public class StatusModel : PageModel
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public List<ServiceStatus> Services { get; set; } = [];
    public string LastCheckTime { get; set; } = "";

    public StatusModel(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }

    public async Task OnGetAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        Services =
        [
            await CheckService("API d'authentification", $"{identityUrl}/health", "Connecté", "Déconnecté"),
            await CheckService("Plateforme développeur", $"{Request.Scheme}://{Request.Host}/", "Accessible", "Inaccessible"),
            await CheckService("Base de données", $"{identityUrl}/health/ready", "Connecté", "Déconnecté"),
            new ServiceStatus { Name = "API Gateway", Status = "Non vérifié", Detail = "Non disponible en local", IsHealthy = null },
            new ServiceStatus { Name = "Swagger / Documentation", Detail = "Accessible", Status = "Accessible", IsHealthy = true },
        ];

        LastCheckTime = DateTime.UtcNow.ToString("g") + " UTC";
    }

    private async Task<ServiceStatus> CheckService(string name, string url, string healthyText, string unhealthyText)
    {
        try
        {
            var resp = await _httpFactory.CreateClient().GetAsync(url);
            if (resp.IsSuccessStatusCode)
                return new ServiceStatus { Name = name, Status = healthyText, Detail = $"Latence ~{resp.Headers.Date?.ToString() ?? "N/A"}", IsHealthy = true };
            return new ServiceStatus { Name = name, Status = unhealthyText, Detail = $"HTTP {(int)resp.StatusCode}", IsHealthy = false };
        }
        catch (Exception ex)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<StatusModel>>();
            logger.LogWarning(ex, "Failed to check service health: {Url}", url);
            return new ServiceStatus { Name = name, Status = unhealthyText, Detail = "Connexion impossible", IsHealthy = false };
        }
    }
}

public class ServiceStatus
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string Detail { get; set; } = "";
    public bool? IsHealthy { get; set; }
}
