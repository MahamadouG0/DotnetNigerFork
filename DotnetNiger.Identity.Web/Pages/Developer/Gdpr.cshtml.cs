using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer;

[Authorize]
public class GdprModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public GdprModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public List<ConsentRecord> Consents { get; set; } = [];
    public string Message { get; set; } = "";
    public bool IsError { get; set; }

    public async Task OnGetAsync()
    {
        await LoadConsentsAsync();
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"{identityUrl}/api/v1/account/data");

        if (response.IsSuccessStatusCode)
        {
            var zipData = await response.Content.ReadAsByteArrayAsync();
            return File(zipData, "application/zip", $"dotnetniger-export-{DateTime.UtcNow:yyyy-MM-dd}.zip");
        }

        Message = "Erreur lors de l'export des données.";
        IsError = true;
        await LoadConsentsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostForgetMeAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync($"{identityUrl}/api/v1/account/forget-me", null);

        if (response.IsSuccessStatusCode)
        {
            Message = "Votre compte a été anonymisé conformément au RGPD. Vous allez être déconnecté.";
            IsError = false;
            return Page();
        }

        var error = await response.Content.ReadAsStringAsync();
        Message = $"Erreur : {error}";
        IsError = true;
        await LoadConsentsAsync();
        return Page();
    }

    private async Task LoadConsentsAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync($"{identityUrl}/api/v1/account/consent");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Consents = JsonSerializer.Deserialize<List<ConsentRecord>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
        }
        catch (Exception ex)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<GdprModel>>();
            logger.LogWarning(ex, "Failed to load consents from Identity API");
        }
    }
}

public class ConsentRecord
{
    public string ConsentType { get; set; } = "";
    public string ConsentVersion { get; set; } = "";
    public bool Granted { get; set; }
    public DateTime CreatedAt { get; set; }
}
