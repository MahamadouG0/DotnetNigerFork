using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace DotnetNiger.Identity.IntegrationTests;

public class AdminEndpointsTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public AdminEndpointsTests(IdentityWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUsers_ReturnsOk_ForAdmin()
    {
        var token = await TestUserFactory.CreateUserTokenAsync(
            _factory.Services,
            "admin@test.com",
            "admin",
            "AdminPassword1!",
            "SuperAdmin");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ReturnsForbidden_ForNonAdmin()
    {
        var token = await TestUserFactory.CreateUserTokenAsync(
            _factory.Services,
            "user@test.com",
            "user",
            "UserPassword1!");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
