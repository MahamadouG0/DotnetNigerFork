using System.Net;
using Xunit;

namespace DotnetNiger.Identity.IntegrationTests;

public class AdminEndpointsTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminEndpointsTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AdminEndpoint_ReturnsUnauthorized_WithoutToken()
    {
        var tenantId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/{tenantId}/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
