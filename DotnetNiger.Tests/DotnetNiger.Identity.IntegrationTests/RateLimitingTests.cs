using System.Net;
using System.Threading.Tasks;
using DotnetNiger.Identity.IntegrationTests;
using Xunit;

namespace DotnetNiger.Identity.IntegrationTests;

public class RateLimitingTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AuthEndpoints_RateLimitIsApplied()
    {
        // Act: Make multiple rapid requests to an AuthController endpoint with [EnableRateLimiting("Auth")]
        var tasks = new Task<HttpResponseMessage>[25]; // Default auth limit is 20/min
        
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = _client.PostAsync("/api/v1/auth/register", 
                new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { 
                        email = $"ratelimit-test-{i}@example.com",
                        password = "Test@123456",
                        firstName = "Rate",
                        lastName = "Limit"
                    }),
                    System.Text.Encoding.UTF8,
                    "application/json"));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert: Most should succeed (under limit), some should be rate limited (429)
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var rateLimitedCount = responses.Count(r => r.StatusCode == (HttpStatusCode)429);
        
        // We should see some rate limiting (exact count depends on test timing)
        Assert.True(rateLimitedCount > 0, "Expected at least some requests to be rate limited (429)");
        Assert.True(successCount > 0, "Expected some requests to succeed");
    }
}