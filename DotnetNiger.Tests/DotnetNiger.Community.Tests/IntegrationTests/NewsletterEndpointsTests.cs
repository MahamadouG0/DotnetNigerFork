using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotnetNiger.Community.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace DotnetNiger.Community.Tests.IntegrationTests;

public class NewsletterEndpointsTests : IClassFixture<CommunityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NewsletterEndpointsTests(CommunityWebApplicationFactory factory)
    {
        _client = factory.HttpClient;
    }

    [Fact]
    public async Task Subscribe_ValidEmail_ReturnsOk()
    {
        var request = new SubscribeRequest("test@example.com", "Test User");

        var response = await _client.PostAsJsonAsync("/api/v1/newsletter/subscribe", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Subscribe_DuplicateEmail_ReturnsConflict()
    {
        var request = new SubscribeRequest("duplicate@example.com", "Duplicate");
        await _client.PostAsJsonAsync("/api/v1/newsletter/subscribe", request);

        var response = await _client.PostAsJsonAsync("/api/v1/newsletter/subscribe", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetActiveCount_ReturnsCount()
    {
        var request = new SubscribeRequest("count@example.com", "Count Test");
        await _client.PostAsJsonAsync("/api/v1/newsletter/subscribe", request);

        var response = await _client.GetAsync("/api/v1/newsletter/count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("activeCount").GetInt32().Should().BeGreaterThan(0);
    }
}
