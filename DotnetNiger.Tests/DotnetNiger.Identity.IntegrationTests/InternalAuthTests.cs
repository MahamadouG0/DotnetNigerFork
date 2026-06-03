using System.Net;
using Xunit;

namespace DotnetNiger.Identity.IntegrationTests;

public class InternalAuthTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InternalAuthTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task InternalActive_WithoutKey_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/external-services/_internal/active");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InternalActive_WithWrongKey_ReturnsUnauthorized()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/external-services/_internal/active");
        request.Headers.Add("X-Internal-Key", "wrong-key");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InternalActive_WithCorrectKey_ReturnsOk()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/external-services/_internal/active");
        request.Headers.Add("X-Internal-Key", "test-internal-key");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
