using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DotnetNiger.Identity.Tests.Infrastructure;

public class EmailSenderTests
{
    private readonly Mock<ILogger<EmailSender>> _loggerMock;
    private readonly SmtpOptions _smtpOptions;
    private readonly EmailSender _sut;

    public EmailSenderTests()
    {
        _loggerMock = new Mock<ILogger<EmailSender>>();
        _smtpOptions = new SmtpOptions { Host = "" };
        _sut = new EmailSender(Options.Create(_smtpOptions), _loggerMock.Object);
    }

    private static ApplicationUser CreateUser(string firstName = "Jean")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@test.com",
            Email = "test@test.com",
            FirstName = firstName,
            LastName = "Dupont"
        };
    }

    private static void VerifyLogCalled(Mock<ILogger<EmailSender>> logger, Times times)
    {
        logger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    [Fact]
    public async Task SendConfirmationLinkAsync_ShouldNotThrow_WhenHostEmpty()
    {
        var user = CreateUser();

        var act = () => _sut.SendConfirmationLinkAsync(user, user.Email!, "https://example.com/confirm");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendConfirmationLinkAsync_ShouldLog_WhenHostEmpty()
    {
        var user = CreateUser();

        await _sut.SendConfirmationLinkAsync(user, user.Email!, "https://example.com/confirm");

        VerifyLogCalled(_loggerMock, Times.Once());
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_ShouldNotThrow_WhenHostEmpty()
    {
        var user = CreateUser();

        var act = () => _sut.SendPasswordResetLinkAsync(user, user.Email!, "https://example.com/reset");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_ShouldLog_WhenHostEmpty()
    {
        var user = CreateUser();

        await _sut.SendPasswordResetLinkAsync(user, user.Email!, "https://example.com/reset");

        VerifyLogCalled(_loggerMock, Times.Once());
    }

    [Fact]
    public async Task SendPasswordResetCodeAsync_ShouldNotThrow_WhenHostEmpty()
    {
        var user = CreateUser();

        var act = () => _sut.SendPasswordResetCodeAsync(user, user.Email!, "123456");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendConfirmationCodeAsync_ShouldNotThrow_WhenHostEmpty()
    {
        var user = CreateUser();

        var act = () => _sut.SendConfirmationCodeAsync(user, user.Email!, "123456", "https://example.com/confirm");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendConfirmationLinkAsync_ShouldIncludeUserFirstNameInTemplate()
    {
        var user = CreateUser("Marie");

        await _sut.SendConfirmationLinkAsync(user, user.Email!, "https://example.com/confirm");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("Marie")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_ShouldIncludeUserFirstNameInTemplate()
    {
        var user = CreateUser("Pierre");

        await _sut.SendPasswordResetLinkAsync(user, user.Email!, "https://example.com/reset");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("Pierre")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldLog_WhenHostEmpty()
    {
        await _sut.SendEmailAsync("test@test.com", "Subject", "<html>Body</html>");

        VerifyLogCalled(_loggerMock, Times.Once());
    }

    [Fact]
    public async Task SendEmailAsync_ShouldNotThrow_WhenHostEmpty()
    {
        var act = () => _sut.SendEmailAsync("test@test.com", "Subject", "<html>Body</html>");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendConfirmationLinkAsync_ShouldIncludeConfirmationLinkInTemplate()
    {
        var user = CreateUser();
        var link = "https://example.com/confirm?token=abc";

        await _sut.SendConfirmationLinkAsync(user, user.Email!, link);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains(link)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_ShouldIncludeResetLinkInTemplate()
    {
        var user = CreateUser();
        var link = "https://example.com/reset?token=xyz";

        await _sut.SendPasswordResetLinkAsync(user, user.Email!, link);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains(link)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
