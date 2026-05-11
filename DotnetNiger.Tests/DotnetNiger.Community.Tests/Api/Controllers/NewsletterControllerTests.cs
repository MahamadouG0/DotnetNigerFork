using DotnetNiger.Community.Api.Controllers;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Models.Requests;
using DotnetNiger.Community.Domain.Models.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Api.Controllers;

public class NewsletterControllerTests
{
    private readonly Mock<INewsletterService> _newsletterServiceMock;
    private readonly NewsletterController _controller;

    public NewsletterControllerTests()
    {
        _newsletterServiceMock = new Mock<INewsletterService>();
        var logger = new Mock<ILogger<NewsletterController>>();
        _controller = new NewsletterController(_newsletterServiceMock.Object, logger.Object);
    }

    [Fact]
    public async Task GetSubscriptions_WhenServiceReturnsData_ReturnsOk()
    {
        // Arrange
        var data = new List<NewsletterSubscriptionResponse>
        {
            new() { Id = Guid.NewGuid(), Email = "a@test.com", IsActive = true, IsVerified = true, CreatedAt = DateTime.UtcNow }
        };

        _newsletterServiceMock
            .Setup(s => s.GetSubscriptionsAsync(true, true))
            .ReturnsAsync(data);

        // Act
        var result = await _controller.GetSubscriptions(true, true);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _newsletterServiceMock.Verify(s => s.GetSubscriptionsAsync(true, true), Times.Once);
    }

    [Fact]
    public async Task UpdateSubscriptionStatus_WhenSubscriptionExists_ReturnsOk()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var request = new NewsletterAdminStatusUpdateRequest
        {
            IsActive = false,
            IsVerified = false
        };

        _newsletterServiceMock
            .Setup(s => s.SetSubscriptionStatusAsync(subscriptionId, false, false))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateSubscriptionStatus(subscriptionId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _newsletterServiceMock.Verify(s => s.SetSubscriptionStatusAsync(subscriptionId, false, false), Times.Once);
    }

    [Fact]
    public async Task UpdateSubscriptionStatus_WhenSubscriptionMissing_ReturnsNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var request = new NewsletterAdminStatusUpdateRequest
        {
            IsActive = true,
            IsVerified = true
        };

        _newsletterServiceMock
            .Setup(s => s.SetSubscriptionStatusAsync(subscriptionId, true, true))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateSubscriptionStatus(subscriptionId, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteSubscription_WhenSubscriptionExists_ReturnsOk()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        _newsletterServiceMock
            .Setup(s => s.DeleteSubscriptionAsync(subscriptionId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteSubscription(subscriptionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _newsletterServiceMock.Verify(s => s.DeleteSubscriptionAsync(subscriptionId), Times.Once);
    }

    [Fact]
    public async Task DeleteSubscription_WhenSubscriptionMissing_ReturnsNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        _newsletterServiceMock
            .Setup(s => s.DeleteSubscriptionAsync(subscriptionId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteSubscription(subscriptionId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
