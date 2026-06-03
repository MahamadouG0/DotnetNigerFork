using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Services;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using FluentAssertions;
using Xunit;

#pragma warning disable CS8604

namespace DotnetNiger.Identity.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly IdentityDbContext _db;
    private readonly Mock<IEmailSender<ApplicationUser>> _emailSenderMock;
    private readonly SmtpOptions _smtpOptions;

    public UserServiceTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _emailSenderMock = new Mock<IEmailSender<ApplicationUser>>();
        _smtpOptions = new SmtpOptions { Host = "smtp.test.com" };

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new IdentityDbContext(options, new TenantContext());
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = Mock.Of<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private UserService CreateSut()
    {
        return new UserService(
            _userManagerMock.Object,
            _db,
            _emailSenderMock.Object,
            Options.Create(_smtpOptions));
    }

    private static ApplicationUser CreateTestUser(Guid? tenantId = null)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            TenantId = tenantId ?? Guid.NewGuid(),
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnUserResponse_WhenSuccessful()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var request = new CreateUserRequest(
            "new@test.com", "Password1!", "New", "User", null, tenantId, new List<string> { "User" });

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRolesAsync(It.IsAny<ApplicationUser>(), request.Roles!))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });

        var result = await sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.Email.Should().Be("new@test.com");
        result.FirstName.Should().Be("New");
        result.LastName.Should().Be("User");
        result.TenantId.Should().Be(tenantId);
        result.Roles.Should().Contain("User");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenPasswordPolicyFails()
    {
        var sut = CreateSut();
        var request = new CreateUserRequest(
            "new@test.com", "weak", "New", "User", null, Guid.NewGuid());

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "Le mot de passe est trop court"
            }));

        var act = () => sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*trop court*");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExistsAndSameTenant()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var user = CreateTestUser(tenantId);
        var roles = new List<string> { "Admin" };

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);

        var result = await sut.GetByIdAsync(tenantId, user.Id);

        result.Should().NotBeNull();
        result!.Email.Should().Be(user.Email);
        result.TenantId.Should().Be(tenantId);
        result.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserFromDifferentTenant()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        var differentTenantId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var result = await sut.GetByIdAsync(differentTenantId, user.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserNotFound()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser)null!);

        var result = await sut.GetByIdAsync(Guid.NewGuid(), userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTenantAsync_ShouldReturnPaginatedResults()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var anotherTenantId = Guid.NewGuid();

        for (int i = 1; i <= 3; i++)
        {
            _db.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = $"user{i}@test.com",
                Email = $"user{i}@test.com",
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                TenantId = tenantId,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        _db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "other@test.com",
            Email = "other@test.com",
            TenantId = anotherTenantId,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var pagination = new PaginationQuery(1, 2);
        var result = await sut.GetByTenantAsync(tenantId, pagination);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetByTenantAsync_ShouldReturnEmpty_WhenNoUsers()
    {
        var sut = CreateSut();
        var pagination = new PaginationQuery(1, 20);

        var result = await sut.GetByTenantAsync(Guid.NewGuid(), pagination);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedUser_WhenSuccessful()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var user = CreateTestUser(tenantId);
        var request = new UpdateUserRequest("Updated", "User", "https://avatar.url", true);

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        var result = await sut.UpdateAsync(tenantId, user.Id, request);

        result.Should().NotBeNull();
        result.FirstName.Should().Be("Updated");
        result.LastName.Should().Be("User");
        user.FirstName.Should().Be("Updated");
        user.LastName.Should().Be("User");
        user.AvatarUrl.Should().Be("https://avatar.url");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenUserNotFound()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser)null!);

        var act = () => sut.UpdateAsync(Guid.NewGuid(), userId,
            new UpdateUserRequest("First", null, null, null));

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Utilisateur non trouvé");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenUserFromDifferentTenant()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        var differentTenantId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var act = () => sut.UpdateAsync(differentTenantId, user.Id,
            new UpdateUserRequest("First", null, null, null));

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Utilisateur non trouvé");
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteUser_WhenExistsAndSameTenant()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var user = CreateTestUser(tenantId);

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        await sut.DeleteAsync(tenantId, user.Id);

        _userManagerMock.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotDelete_WhenUserFromDifferentTenant()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        var differentTenantId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        await sut.DeleteAsync(differentTenantId, user.Id);

        _userManagerMock.Verify(x => x.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenUserNotFound()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser)null!);

        await sut.DeleteAsync(Guid.NewGuid(), userId);

        _userManagerMock.Verify(x => x.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldSucceed_WhenCurrentPasswordCorrect()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var user = CreateTestUser(tenantId);
        var request = new ChangePasswordRequest("oldPass", "newPass");

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, "oldPass", "newPass"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        var result = await sut.ChangePasswordAsync(tenantId, user.Id, request);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrow_WhenCurrentPasswordIncorrect()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var user = CreateTestUser(tenantId);
        var request = new ChangePasswordRequest("wrongPass", "newPass");

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, "wrongPass", "newPass"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordMismatch",
                Description = "Mot de passe actuel incorrect"
            }));

        var act = () => sut.ChangePasswordAsync(tenantId, user.Id, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*incorrect*");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrow_WhenUserNotFound()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser)null!);

        var act = () => sut.ChangePasswordAsync(Guid.NewGuid(), userId,
            new ChangePasswordRequest("old", "new"));

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Utilisateur non trouvé");
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldSendEmail_WhenUserExists()
    {
        var sut = CreateSut();
        var user = CreateTestUser();

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token");

        await sut.ForgotPasswordAsync(user.Email);

        _emailSenderMock.Verify(
            x => x.SendPasswordResetLinkAsync(
                user, user.Email, It.Is<string>(s => s.Contains("reset-token"))),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldThrow_WhenUserNotFound()
    {
        var sut = CreateSut();

        _userManagerMock.Setup(x => x.FindByEmailAsync("unknown@test.com"))
            .ReturnsAsync((ApplicationUser)null!);

        var act = () => sut.ForgotPasswordAsync("unknown@test.com");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Utilisateur non trouvé");
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldSucceed_WhenValidToken()
    {
        var sut = CreateSut();
        var user = CreateTestUser();

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "valid-token", "newPass"))
            .ReturnsAsync(IdentityResult.Success);

        await sut.ResetPasswordAsync(user.Email, "valid-token", "newPass");
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrow_WhenInvalidToken()
    {
        var sut = CreateSut();
        var user = CreateTestUser();

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "invalid-token", "newPass"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidToken",
                Description = "Token invalide"
            }));

        var act = () => sut.ResetPasswordAsync(user.Email, "invalid-token", "newPass");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Token*");
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrow_WhenUserNotFound()
    {
        var sut = CreateSut();

        _userManagerMock.Setup(x => x.FindByEmailAsync("unknown@test.com"))
            .ReturnsAsync((ApplicationUser)null!);

        var act = () => sut.ResetPasswordAsync("unknown@test.com", "token", "newPass");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Utilisateur non trouvé");
    }
}
