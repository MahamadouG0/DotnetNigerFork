using System.Net;
using System.Net.Http.Json;
using DotnetNiger.Identity.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace DotnetNiger.Identity.IntegrationTests;

public class AdminCrudTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminCrudTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        
        // Login as admin to get token
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
        var refreshToken = json.GetProperty("refresh_token").GetString();
        var tokenType = json.GetProperty("token_type").GetString();
        var expiresIn = json.GetProperty("expires_in").GetInt32();
        
        var tokenResult = new DotnetNiger.Identity.Application.DTOs.TokenResponse(
            accessToken!, refreshToken!, tokenType!, expiresIn,
            Guid.Empty, "", null, new List<string>());
        
        tokenResult.AccessToken.Should().NotBeNullOrEmpty();
        
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);
    }

    [Fact]
    public async Task Admin_CanCreateTenant()
    {
        // Arrange
        var createRequest = new
        {
            name = "Test Tenant",
            slug = "test-tenant-" + Guid.NewGuid().ToString().Substring(0, 8),
            description = "A test tenant"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/admin/tenants", createRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdTenant = await response.Content.ReadFromJsonAsync<DotnetNiger.Identity.Application.DTOs.TenantResponse>();
        createdTenant.Should().NotBeNull();
        createdTenant!.Name.Should().Be("Test Tenant");
        createdTenant.Slug.Should().Be(createRequest.slug);
        createdTenant.Description.Should().Be("A test tenant");
        createdTenant.IsActive.Should().BeTrue();
        createdTenant.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Admin_CanGetTenants()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/admin/tenants");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
         var result = await response.Content.ReadFromJsonAsync<DotnetNiger.Identity.Application.DTOs.PaginatedResponse<DotnetNiger.Identity.Application.DTOs.TenantResponse>>();
         result.Should().NotBeNull();
         result.Items.Should().HaveCountGreaterThanOrEqualTo(1); // At least the default tenant
    }

    [Fact]
    public async Task Admin_CanUpdateTenant()
    {
        // Arrange: Create a tenant first
        var createRequest = new
        {
            name = "Tenant To Update",
            slug = "tenant-to-update-" + Guid.NewGuid().ToString().Substring(0, 8),
            description = "Original description"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/v1/admin/tenants", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<DotnetNiger.Identity.Application.DTOs.TenantResponse>();
        createdTenant.Should().NotBeNull();
        var tenantId = createdTenant.Id;
        
        // Act: Update the tenant
        var updateRequest = new
        {
            name = "Updated Tenant Name",
            description = "Updated description"
        };
        
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/admin/tenants/{tenantId}", updateRequest);
        
        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var updatedTenant = await updateResponse.Content.ReadFromJsonAsync<DotnetNiger.Identity.Application.DTOs.TenantResponse>();
        updatedTenant.Should().NotBeNull();
        updatedTenant!.Id.Should().Be(tenantId);
        updatedTenant.Name.Should().Be("Updated Tenant Name");
        updatedTenant.Description.Should().Be("Updated description");
        // Slug should remain unchanged
        updatedTenant.Slug.Should().Be(createRequest.slug);
    }

    [Fact]
    public async Task Admin_CanToggleTenantActiveStatus()
    {
        // Arrange: Create a tenant first
        var createRequest = new
        {
            name = "Tenant To Toggle",
            slug = "tenant-to-toggle-" + Guid.NewGuid().ToString().Substring(0, 8),
            description = "A test tenant"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/v1/admin/tenants", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<DotnetNiger.Identity.Application.DTOs.TenantResponse>();
        createdTenant.Should().NotBeNull();
        var tenantId = createdTenant.Id;
        
        // Act: Deactivate the tenant
        var deactivateResponse = await _client.PutAsJsonAsync($"/api/v1/admin/tenants/{tenantId}", new { isActive = false });
        
        // Assert
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify it's deactivated
        var getResponse = await _client.GetAsync($"/api/v1/admin/tenants/{tenantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var tenant = await getResponse.Content.ReadFromJsonAsync<DotnetNiger.Identity.Application.DTOs.TenantResponse>();
        tenant.Should().NotBeNull();
        tenant!.IsActive.Should().BeFalse();
        
        // Act: Reactivate the tenant
        var activateResponse = await _client.PutAsJsonAsync($"/api/v1/admin/tenants/{tenantId}", new { isActive = true });
        
        // Assert
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify it's activated
        getResponse = await _client.GetAsync($"/api/v1/admin/tenants/{tenantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        tenant = await getResponse.Content.ReadFromJsonAsync<DotnetNiger.Identity.Application.DTOs.TenantResponse>();
        tenant.Should().NotBeNull();
        tenant!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Admin_CanDeleteTenant()
    {
        // Arrange: Create a tenant first
        var createRequest = new
        {
            name = "Tenant To Delete",
            slug = "tenant-to-delete-" + Guid.NewGuid().ToString().Substring(0, 8),
            description = "A test tenant"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/v1/admin/tenants", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<DotnetNiger.Identity.Application.DTOs.TenantResponse>();
        createdTenant.Should().NotBeNull();
        var tenantId = createdTenant.Id;
        
        // Act: Delete the tenant
        var deleteResponse = await _client.DeleteAsync($"/api/v1/admin/tenants/{tenantId}");
        
        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/v1/admin/tenants/{tenantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}