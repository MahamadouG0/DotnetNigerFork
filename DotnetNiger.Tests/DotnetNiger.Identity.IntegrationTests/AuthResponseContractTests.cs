using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DotnetNiger.Identity.IntegrationTests;

public class AuthResponseContractTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public AuthResponseContractTests(IdentityWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Response_UsesSingleApiSuccessEnvelope()
    {
        const string email = "contract-login@test.com";
        const string username = "contract-login";
        const string password = "ContractPwd1!";

        await TestUserFactory.CreateUserTokenAsync(_factory.Services, email, username, password);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
        Assert.True(root.TryGetProperty("message", out var message));
        Assert.Equal("Login successful.", message.GetString());

        var data = root.GetProperty("data");
        Assert.True(data.TryGetProperty("user", out _));
        Assert.True(data.TryGetProperty("token", out _));

        // Contract guard: business DTO should not duplicate envelope fields.
        Assert.False(data.TryGetProperty("success", out _));
        Assert.False(data.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Refresh_Response_UsesSingleApiSuccessEnvelope()
    {
        const string email = "contract-refresh@test.com";
        const string username = "contract-refresh";
        const string password = "ContractPwd1!";

        await TestUserFactory.CreateUserTokenAsync(_factory.Services, email, username, password);

        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginPayload = await loginResponse.Content.ReadAsStringAsync();
        using var loginDoc = JsonDocument.Parse(loginPayload);
        var refreshToken = loginDoc.RootElement
            .GetProperty("data")
            .GetProperty("token")
            .GetProperty("refreshToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(refreshToken));

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            RefreshToken = refreshToken
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshPayload = await refreshResponse.Content.ReadAsStringAsync();
        using var refreshDoc = JsonDocument.Parse(refreshPayload);
        var root = refreshDoc.RootElement;

        Assert.True(root.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
        Assert.True(root.TryGetProperty("message", out var message));
        Assert.Equal("Token refreshed.", message.GetString());

        var data = root.GetProperty("data");
        Assert.True(data.TryGetProperty("user", out _));
        Assert.True(data.TryGetProperty("token", out _));
        Assert.False(data.TryGetProperty("success", out _));
        Assert.False(data.TryGetProperty("message", out _));
    }
}
