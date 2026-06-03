using System.Net;
using Xunit;

namespace DotnetNiger.Identity.IntegrationTests;

public class HealthEndpointTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/ready");
        // Could be 200 (healthy) or 503 (unhealthy) depending on DB state
        // In test environment with in-memory SQLite, should be 200
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthDownstream_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/downstream");
        // Could be 200 (healthy) or 503 (unhealthy) depending on DB state
        // In test environment with in-memory SQLite, should be 200
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}