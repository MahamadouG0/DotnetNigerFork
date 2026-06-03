using System.Net;
using System.Net.Http.Json;
using DotnetNiger.Identity.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace DotnetNiger.Identity.IntegrationTests;

public class ExternalServicesTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ExternalServicesTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        LoginAsync().GetAwaiter().GetResult();
    }

    private async Task LoginAsync()
    {
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "test-client",
            ["username"] = "admin@dotnetniger.com",
            ["password"] = "Admin@123456",
            ["scope"] = "openid profile email roles offline_access"
        });
        
        var tokenResponse = await _client.PostAsync("/connect/token", tokenRequest);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var json = await tokenResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var accessToken = json.GetProperty("access_token").GetString();
        accessToken.Should().NotBeNullOrEmpty();
        
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

        [Fact]
        public async Task PatchExternalService_UpdatesServiceSuccessfully()
        {
            // Arrange: Create an API key first (required by ExternalServicesController)
            var userInfo = await _client.GetAsync("/api/v1/auth/userinfo");
            userInfo.StatusCode.Should().Be(HttpStatusCode.OK);
            var userData = await userInfo.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var tenantId = userData.GetProperty("tenantId").GetString()!;

            var createApiKeyResponse = await _client.PostAsJsonAsync(
                $"/api/v1/admin/tenants/{tenantId}/api-keys",
                new { name = "Test Key" });
            createApiKeyResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Register a service
            var registerRequest = new RegisterExternalServiceRequest(
                Name: "Test Service",
                Slug: "test-service",
                BaseUrl: "https://example.com",
                Description: "Original description",
                HealthEndpoint: "/health");

            var registerResponse = await _client.PostAsJsonAsync("/api/v1/external-services/register", registerRequest);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var registeredService = await registerResponse.Content.ReadFromJsonAsync<ExternalServiceResponse>();
            registeredService.Should().NotBeNull();
            registeredService!.Id.Should().NotBeEmpty();

            // Act: Update the service via PATCH
            var updateRequest = new UpdateExternalServiceRequest(
                BaseUrl: null,
                Description: "Updated description",
                HealthEndpoint: null);

            var updateResponse = await _client.PatchAsJsonAsync($"/api/v1/external-services/{registeredService.Id}", updateRequest);
            
            // Assert
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedService = await updateResponse.Content.ReadFromJsonAsync<ExternalServiceResponse>();
            updatedService.Should().NotBeNull();
            updatedService!.Id.Should().Be(registeredService.Id);
            updatedService.Name.Should().Be("Test Service"); // Name cannot be updated via UpdateExternalServiceRequest? Wait, the request doesn't have Name property. So Name should remain unchanged.
            updatedService.Description.Should().Be("Updated description");
            // Unchanged fields should remain the same
            updatedService.Slug.Should().Be("test-service");
            updatedService.BaseUrl.Should().Be("https://example.com");
            updatedService.HealthEndpoint.Should().Be("/health");
        }

        [Fact]
        public async Task PatchExternalService_ReturnsNotFound_WhenServiceDoesNotExist()
        {
            // Arrange
            var updateRequest = new UpdateExternalServiceRequest(
                BaseUrl: null,
                Description: "Non-existent Service",
                HealthEndpoint: null);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/v1/external-services/{Guid.NewGuid()}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
}