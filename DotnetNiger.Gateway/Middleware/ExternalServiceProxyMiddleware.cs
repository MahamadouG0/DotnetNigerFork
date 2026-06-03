using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace DotnetNiger.Gateway.Middleware;

public partial class ExternalServiceProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _identityBaseUrl;
    private readonly TimeSpan _cacheDuration;

    public ExternalServiceProxyMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _next = next;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _identityBaseUrl = (configuration["DeveloperPortal:IdentityBaseUrl"]
            ?? "http://localhost:5075").TrimEnd('/');
        var cacheSeconds = int.TryParse(
            configuration["DeveloperPortal:SlugCacheDurationSeconds"], out var s) ? s : 60;
        _cacheDuration = TimeSpan.FromSeconds(cacheSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var match = ExtPathRegex().Match(path);
        if (!match.Success)
        {
            await _next(context);
            return;
        }

        var slug = match.Groups[1].Value;
        var remainingPath = match.Groups[2].Value;
        var cacheKey = $"ext:{slug}";

        var baseUrl = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetAsync(
                    $"{_identityBaseUrl}/api/v1/external-services/by-slug/{slug}",
                    context.RequestAborted);

                if (!response.IsSuccessStatusCode) return null;

                var result = await response.Content
                    .ReadFromJsonAsync<ServiceLookupResult>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                        context.RequestAborted);

                return result?.BaseUrl;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to resolve external service slug: {Slug}", slug);
                return null;
            }
        });

        if (baseUrl == null)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = "Service not found or not active" }),
                context.RequestAborted);
            return;
        }

        var targetUrl = $"{baseUrl.TrimEnd('/')}/{remainingPath.TrimStart('/')}{context.Request.QueryString}";

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var requestMessage = new HttpRequestMessage(
                new HttpMethod(context.Request.Method), targetUrl);

            foreach (var header in context.Request.Headers)
            {
                if (!header.Key.StartsWith("Host", StringComparison.OrdinalIgnoreCase))
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.Headers.Host = new Uri(baseUrl).Host;

            if (context.Request.Body is { CanSeek: true, Length: >0 } or { CanSeek: false })
            {
                requestMessage.Content = new StreamContent(context.Request.Body);
                if (context.Request.ContentType != null)
                    requestMessage.Content.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(context.Request.ContentType);
            }

            using var response = await client.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);

            context.Response.StatusCode = (int)response.StatusCode;

            foreach (var header in response.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in response.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            context.Response.Headers.Remove("Transfer-Encoding");

            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            if (!context.Response.HasStarted)
                context.Response.StatusCode = 504;
        }
        catch (HttpRequestException ex)
        {
            Log.Warning(ex, "Proxy error for external service {Slug} -> {Target}", slug, targetUrl);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 502;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { error = "Upstream service unavailable" }),
                    context.RequestAborted);
            }
        }
    }

    [GeneratedRegex(@"^/ext/([^/]+)(/.*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExtPathRegex();

    private sealed record ServiceLookupResult(string BaseUrl);
}
