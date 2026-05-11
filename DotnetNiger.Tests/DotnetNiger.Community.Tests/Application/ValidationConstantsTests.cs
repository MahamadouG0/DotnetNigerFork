using DotnetNiger.Community.Application.ValidationConstants;
using FluentAssertions;
using Xunit;

namespace DotnetNiger.Community.Tests.Application;

public class ValidationConstantsTests
{
    [Fact]
    public void DefaultPageSize_ShouldBeValid()
    {
        // Assert
        ValidationConstants.DefaultPageSize.Should().Be(10);
    }

    [Fact]
    public void MaxPageSize_ShouldBeGreaterThanDefault()
    {
        // Assert
        ValidationConstants.MaxPageSize.Should().BeGreaterThan(ValidationConstants.DefaultPageSize);
        ValidationConstants.MaxPageSize.Should().Be(100);
    }

    [Fact]
    public void MaxTitleLength_ShouldBeReasonable()
    {
        // Assert
        ValidationConstants.MaxTitleLength.Should().BeGreaterThan(10);
        ValidationConstants.MaxTitleLength.Should().BeLessThanOrEqualTo(500);
    }

    [Fact]
    public void MinPasswordLength_ShouldMeetSecurityStandards()
    {
        // Assert
        ValidationConstants.MinPasswordLength.Should().BeGreaterThanOrEqualTo(8);
    }

    [Fact]
    public void MaxContentLength_ShouldBeReasonable()
    {
        // Assert
        ValidationConstants.MaxContentLength.Should().BeGreaterThan(ValidationConstants.MaxTitleLength);
    }

    [Fact]
    public void ValidationMessages_ShouldBeConsistent()
    {
        // Assert
        ValidationMessages.TitleRequired.Should().NotBeNullOrEmpty();
        ValidationMessages.TitleTooLong.Should().NotBeNullOrEmpty();
        ValidationMessages.ContentRequired.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RateLimitConstants_ShouldBeValid()
    {
        // Assert
        ValidationConstants.LoginAttemptsLimit.Should().BeGreaterThan(0);
        ValidationConstants.RegisterAttemptsLimit.Should().BeGreaterThan(0);
        ValidationConstants.RateLimitWindowSeconds.Should().BeGreaterThan(0);
    }
}
