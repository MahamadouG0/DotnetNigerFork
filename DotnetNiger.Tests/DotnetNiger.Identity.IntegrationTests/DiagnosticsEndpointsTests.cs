using System.Net;
using Xunit;

namespace DotnetNiger.Identity.IntegrationTests;

public class DiagnosticsEndpointsTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DiagnosticsEndpointsTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ping_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/diagnostics/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/diagnostics/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
