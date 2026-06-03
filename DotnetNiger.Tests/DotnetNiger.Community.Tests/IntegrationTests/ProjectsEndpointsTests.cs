using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DotnetNiger.Community.Tests.IntegrationTests;

public class ProjectsEndpointsTests : IClassFixture<CommunityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProjectsEndpointsTests(CommunityWebApplicationFactory factory)
    {
        _client = factory.HttpClient;
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/projects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetFeatured_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/projects/featured");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/projects/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
