using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DotnetNiger.Identity.IntegrationTests;

public class TenantRegistrationFlowTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantRegistrationFlowTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterTenant_CreatesTenantAndReturnsIds()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register-tenant", new
        {
            companyName = "IntegrationTest",
            slug = "integration-test",
            adminEmail = "admin@integration-test.com",
            adminPassword = "Test@123!",
            adminFirstName = "Test",
            adminLastName = "User"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("tenantId", out var tenantId));
        Assert.NotEqual("00000000-0000-0000-0000-000000000000", tenantId.GetString());
        Assert.True(body.TryGetProperty("clientId", out _));
        Assert.True(body.TryGetProperty("clientSecret", out _));
        Assert.True(body.TryGetProperty("apiKeySecret", out _));
    }

    [Fact]
    public async Task RegisterTenant_DuplicateSlug_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register-tenant", new
        {
            companyName = "Original",
            slug = "dup-test",
            adminEmail = "orig@dup-test.com",
            adminPassword = "Test@123!",
            adminFirstName = "O",
            adminLastName = "Dup"
        });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register-tenant", new
        {
            companyName = "Duplicate",
            slug = "dup-test",
            adminEmail = "dup@dup-test.com",
            adminPassword = "Test@123!",
            adminFirstName = "D",
            adminLastName = "Dup"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RegisterTenant_DuplicateEmail_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register-tenant", new
        {
            companyName = "First",
            slug = "first-tenant",
            adminEmail = "shared@tenant.com",
            adminPassword = "Test@123!",
            adminFirstName = "F",
            adminLastName = "User"
        });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register-tenant", new
        {
            companyName = "Second",
            slug = "second-tenant",
            adminEmail = "shared@tenant.com",
            adminPassword = "Test@123!",
            adminFirstName = "S",
            adminLastName = "User"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
