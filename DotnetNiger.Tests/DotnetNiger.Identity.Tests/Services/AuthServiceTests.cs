using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using DotnetNiger.Identity.Application.Services;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using FluentAssertions;
using Xunit;

#pragma warning disable CS8604, CS8602

namespace DotnetNiger.Identity.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly TenantContext _tenantContext;
    private readonly IdentityDbContext _db;
    private readonly Mock<IEmailSender<ApplicationUser>> _emailSenderMock;
    private readonly SmtpOptions _smtpOptions;

    public AuthServiceTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _signInManagerMock = CreateSignInManagerMock();
        _tenantContext = new TenantContext();
        _emailSenderMock = new Mock<IEmailSender<ApplicationUser>>();
        _smtpOptions = new SmtpOptions { Host = "smtp.test.com" };

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new IdentityDbContext(options, _tenantContext);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = Mock.Of<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock()
    {
        var accessor = Mock.Of<IHttpContextAccessor>();
        var claimsFactory = Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>();
        return new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            accessor,
            claimsFactory,
            null!, null!, null!, null!);
    }

    private AuthService CreateSut()
    {
        return new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tenantContext,
            _db,
            _emailSenderMock.Object,
            Options.Create(_smtpOptions));
    }

    private static ApplicationUser CreateTestUser()
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            TenantId = Guid.NewGuid(),
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldReturnUserAndRoles_WhenCredentialsValid()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        var roles = new List<string> { "User" };

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "password", true))
            .ReturnsAsync(SignInResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);

        var (resultUser, resultRoles) = await sut.ValidateCredentialsAsync(user.Email, "password");

        resultUser.Should().NotBeNull();
        resultUser.Email.Should().Be(user.Email);
        resultRoles.Should().BeEquivalentTo(roles);
        _tenantContext.TenantId.Should().Be(user.TenantId);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldThrow_WhenUserNotFound()
    {
        var sut = CreateSut();
        _userManagerMock.Setup(x => x.FindByEmailAsync("unknown@test.com"))
            .ReturnsAsync((ApplicationUser)null!);

        var act = () => sut.ValidateCredentialsAsync("unknown@test.com", "password");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Email ou mot de passe incorrect");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldThrow_WhenUserNotActive()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        user.IsActive = false;

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var act = () => sut.ValidateCredentialsAsync(user.Email, "password");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Email ou mot de passe incorrect");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldThrow_WhenEmailNotConfirmed()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        user.EmailConfirmed = false;

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(false);

        var act = () => sut.ValidateCredentialsAsync(user.Email, "password");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Email non confirmé");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldThrow_WhenPasswordInvalid()
    {
        var sut = CreateSut();
        var user = CreateTestUser();

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "wrong", true))
            .ReturnsAsync(SignInResult.Failed);

        var act = () => sut.ValidateCredentialsAsync(user.Email, "wrong");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Email ou mot de passe incorrect");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldThrow_WhenAccountLockedOut()
    {
        var sut = CreateSut();
        var user = CreateTestUser();

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "password", true))
            .ReturnsAsync(SignInResult.LockedOut);

        var act = () => sut.ValidateCredentialsAsync(user.Email, "password");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Compte temporairement verrouillé");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldThrow_WhenTenantMismatch()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        var wrongTenantId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

        var act = () => sut.ValidateCredentialsAsync(user.Email, "password", wrongTenantId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Utilisateur non trouvé dans ce tenant");
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenSuccessful()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        _db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Slug = "test" });
        await _db.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((ApplicationUser)null!);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "Password1!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var (user, code) = await sut.RegisterAsync("new@test.com", "Password1!", "New", "User", tenantId);

        user.Should().NotBeNull();
        user.Email.Should().Be("new@test.com");
        code.Should().NotBeNullOrEmpty();
        code.Length.Should().Be(6);
        _tenantContext.TenantId.Should().Be(tenantId);
        _emailSenderMock.Verify(
            x => x.SendConfirmationLinkAsync(
                It.IsAny<ApplicationUser>(), "new@test.com", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenDuplicateEmail()
    {
        var sut = CreateSut();
        var existingUser = CreateTestUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync(existingUser.Email)).ReturnsAsync(existingUser);

        var act = () => sut.RegisterAsync(existingUser.Email, "Password1!", "New", "User");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Un compte avec cet email existe déjà");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenNoTenantFound()
    {
        var sut = CreateSut();
        _userManagerMock.Setup(x => x.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((ApplicationUser)null!);

        var act = () => sut.RegisterAsync("new@test.com", "Password1!", "New", "User");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Aucun tenant trouvé");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenCreateUserFails()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        _db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Slug = "test" });
        await _db.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((ApplicationUser)null!);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "Le mot de passe est trop court"
            }));

        var act = () => sut.RegisterAsync("new@test.com", "weak", "New", "User", tenantId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*mot de passe*");
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldConfirmEmail_WhenValidCode()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        var hashedCode = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("ABC123")));
        user.EmailConfirmed = false;
        user.EmailConfirmationCode = hashedCode;
        user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(10);

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await sut.ConfirmEmailAsync(user.Email, "ABC123");

        user.EmailConfirmed.Should().BeTrue();
        user.EmailConfirmationCode.Should().BeNull();
        user.EmailConfirmationCodeExpiry.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldThrow_WhenExpiredCode()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        user.EmailConfirmationCode = "EXPIRED_CODE";
        user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(-5);

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var act = () => sut.ConfirmEmailAsync(user.Email, "ABC123");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Code de confirmation expiré");
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldThrow_WhenEmailAlreadyConfirmed()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        user.EmailConfirmed = true;

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var act = () => sut.ConfirmEmailAsync(user.Email, "ABC123");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email déjà confirmé");
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldThrow_WhenInvalidCode()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        user.EmailConfirmationCode = "ABC123";
        user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(10);

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var act = () => sut.ConfirmEmailAsync(user.Email, "WRONG");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Code de confirmation invalide");
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldThrow_WhenUserNotFound()
    {
        var sut = CreateSut();
        _userManagerMock.Setup(x => x.FindByEmailAsync("unknown@test.com"))
            .ReturnsAsync((ApplicationUser)null!);

        var act = () => sut.ConfirmEmailAsync("unknown@test.com", "ABC123");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Utilisateur non trouvé");
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldThrow_WhenNoConfirmationCodeExists()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        user.EmailConfirmationCode = null;
        user.EmailConfirmationCodeExpiry = null;

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var act = () => sut.ConfirmEmailAsync(user.Email, "ABC123");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Aucun code de confirmation trouvé");
    }

    [Fact]
    public async Task ResendConfirmationCodeAsync_ShouldResendCode_WhenValid()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        user.EmailConfirmed = false;

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await sut.ResendConfirmationCodeAsync(user.Email);

        user.EmailConfirmationCode.Should().NotBeNullOrEmpty();
        user.EmailConfirmationCode.Length.Should().Be(64);
        user.EmailConfirmationCodeExpiry.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
        _emailSenderMock.Verify(
            x => x.SendConfirmationLinkAsync(
                It.IsAny<ApplicationUser>(), user.Email, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendConfirmationCodeAsync_ShouldThrow_WhenEmailAlreadyConfirmed()
    {
        var sut = CreateSut();
        var user = CreateTestUser();
        user.EmailConfirmed = true;

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var act = () => sut.ResendConfirmationCodeAsync(user.Email);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email déjà confirmé");
    }

    [Fact]
    public async Task ResendConfirmationCodeAsync_ShouldThrow_WhenUserNotFound()
    {
        var sut = CreateSut();
        _userManagerMock.Setup(x => x.FindByEmailAsync("unknown@test.com"))
            .ReturnsAsync((ApplicationUser)null!);

        var act = () => sut.ResendConfirmationCodeAsync("unknown@test.com");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Utilisateur non trouvé");
    }
}
