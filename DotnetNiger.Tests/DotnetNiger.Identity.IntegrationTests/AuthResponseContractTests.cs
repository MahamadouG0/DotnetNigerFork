using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace DotnetNiger.Identity.IntegrationTests;

public class AuthResponseContractTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthResponseContractTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/connect/token", new
        {
            grant_type = "password",
            username = "nonexistent@test.com",
            password = "WrongPassword1!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
