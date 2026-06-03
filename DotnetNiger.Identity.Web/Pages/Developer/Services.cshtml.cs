using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer;

[Authorize]
public class ServicesModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public ServicesModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public List<ServiceItem> Services { get; set; } = [];
    public string Message { get; set; } = "";
    public bool IsError { get; set; }

    [BindProperty]
    public ServiceInput Input { get; set; } = new();

    [BindProperty]
    public ServiceEditInput EditInput { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadServicesAsync();
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
            name = Input.Name,
            slug = Input.Slug,
            baseUrl = Input.BaseUrl,
            description = Input.Description ?? "",
            healthEndpoint = Input.HealthEndpoint ?? ""
        });

        var response = await client.PostAsync(
            $"{identityUrl}/api/v1/external-services/register",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            Message = "Service enregistré avec succès !";
            IsError = false;
            Input = new ServiceInput();
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadServicesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid serviceId)
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync($"{identityUrl}/api/v1/external-services/{serviceId}");
        if (response.IsSuccessStatusCode)
        {
            Message = "Service supprimé.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadServicesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(Guid serviceId)
    {
        if (!ModelState.IsValid)
        {
            await LoadServicesAsync();
            return Page();
        }

        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(EditInput.BaseUrl)) body["baseUrl"] = EditInput.BaseUrl;
        if (EditInput.Description != null) body["description"] = EditInput.Description;
        if (!string.IsNullOrEmpty(EditInput.HealthEndpoint)) body["healthEndpoint"] = EditInput.HealthEndpoint;

        var request = new HttpRequestMessage(HttpMethod.Patch, $"{identityUrl}/api/v1/external-services/{serviceId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            Message = "Service mis à jour.";
            IsError = false;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Message = $"Erreur : {error}";
            IsError = true;
        }

        await LoadServicesAsync();
        return Page();
    }

    private async Task LoadServicesAsync()
    {
        var identityUrl = _config["Identity:BaseUrl"]?.TrimEnd('/');
        var client = _http.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync($"{identityUrl}/api/v1/external-services");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Services = JsonSerializer.Deserialize<List<ServiceItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
        }
        catch (Exception ex)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<ServicesModel>>();
            logger.LogWarning(ex, "Failed to load external services from Identity API");
            Services = [];
        }
    }
}

public class ServiceInput
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string Description { get; set; } = "";
    public string HealthEndpoint { get; set; } = "";
}

public class ServiceEditInput
{
    public string? BaseUrl { get; set; }
    public string? Description { get; set; }
    public string? HealthEndpoint { get; set; }
}

public class ServiceItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string HealthEndpoint { get; set; } = "/health";
    public string Status { get; set; } = "Pending";
    public int HealthCheckFailures { get; set; }
    public DateTime? LastHealthCheckAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
