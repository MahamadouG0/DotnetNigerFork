using System.Security.Claims;
using DotnetNiger.Identity.Api.Controllers;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Exceptions;
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Infrastructure.External;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DotnetNiger.Identity.Tests;

public class UsersControllerTests
{
	[Fact]
	public async Task UploadAvatar_Throws_WhenFileTooLarge()
	{
		var userId = Guid.NewGuid();
		var options = new FileUploadOptions
		{
			MaxAvatarBytes = 10,
			AllowedAvatarContentTypes = new[] { "image/png" }
		};

		var controller = CreateController(userId, options);
		var file = CreateFormFile(new byte[11], "image/png", "avatar.png");

		var action = async () => await controller.UploadAvatar(file);

		var exception = await Assert.ThrowsAsync<IdentityException>(action);
		exception.StatusCode.Should().Be(400);
	}

	[Fact]
	public async Task UploadAvatar_Throws_WhenContentTypeInvalid()
	{
		var userId = Guid.NewGuid();
		var options = new FileUploadOptions
		{
			MaxAvatarBytes = 1024,
			AllowedAvatarContentTypes = new[] { "image/png" }
		};

		var controller = CreateController(userId, options);
		var file = CreateFormFile(new byte[10], "text/plain", "avatar.txt");

		var action = async () => await controller.UploadAvatar(file);

		var exception = await Assert.ThrowsAsync<IdentityException>(action);
		exception.StatusCode.Should().Be(400);
	}

	[Fact]
	public async Task UploadAvatar_ReturnsUpdatedProfile()
	{
		var userId = Guid.NewGuid();
		var options = new FileUploadOptions
		{
			MaxAvatarBytes = 1024,
			AllowedAvatarContentTypes = new[] { "image/png" },
			AllowedAvatarExtensions = new[] { ".png" },
			PublicBasePath = "/uploads"
		};

		var userService = new Mock<IUserService>();
		var fileUploadService = new Mock<IFileUploadService>();
		var currentProfile = new UserDto { Id = userId, AvatarUrl = "" };
		var expectedUrl = "http://localhost/uploads/avatars/test.png";
		var expectedUser = new UserDto { Id = userId, AvatarUrl = expectedUrl };

		userService
			.Setup(service => service.GetProfileAsync(userId))
			.ReturnsAsync(currentProfile);

		fileUploadService
			.Setup(service => service.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/png"))
			.ReturnsAsync("/uploads/avatars/test.png");

		userService
			.Setup(service => service.UpdateAvatarAsync(userId, expectedUrl))
			.ReturnsAsync(expectedUser);

		var controller = CreateController(userId, options, userService.Object, fileUploadService.Object);
		var file = CreateFormFile(new byte[10], "image/png", "avatar.png");

		var result = await controller.UploadAvatar(file);

		var okResult = result.Result as OkObjectResult;
		okResult.Should().NotBeNull();
		okResult!.Value.Should().BeEquivalentTo(expectedUser);
		userService.Verify(service => service.UpdateAvatarAsync(userId, expectedUrl), Times.Once);
	}

	[Fact]
	public async Task UploadAvatar_Throws_WhenExtensionNotAllowed()
	{
		var userId = Guid.NewGuid();
		var options = new FileUploadOptions
		{
			MaxAvatarBytes = 1024,
			AllowedAvatarContentTypes = new[] { "image/png" },
			AllowedAvatarExtensions = new[] { ".png" }
		};

		var controller = CreateController(userId, options);
		var file = CreateFormFile(new byte[10], "image/png", "avatar.gif");

		var action = async () => await controller.UploadAvatar(file);

		var exception = await Assert.ThrowsAsync<IdentityException>(action);
		exception.StatusCode.Should().Be(400);
	}

	[Fact]
	public async Task UploadAvatar_DeletesPreviousAvatar()
	{
		var userId = Guid.NewGuid();
		var options = new FileUploadOptions
		{
			MaxAvatarBytes = 1024,
			AllowedAvatarContentTypes = new[] { "image/png" }
		};

		var userService = new Mock<IUserService>();
		var fileUploadService = new Mock<IFileUploadService>();
		var previousUrl = "http://localhost/uploads/avatars/old.png";

		userService
			.Setup(service => service.GetProfileAsync(userId))
			.ReturnsAsync(new UserDto { Id = userId, AvatarUrl = previousUrl });

		fileUploadService
			.Setup(service => service.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/png"))
			.ReturnsAsync("/uploads/avatars/new.png");

		userService
			.Setup(service => service.UpdateAvatarAsync(userId, It.IsAny<string>()))
			.ReturnsAsync(new UserDto { Id = userId, AvatarUrl = "http://localhost/uploads/avatars/new.png" });

		var controller = CreateController(userId, options, userService.Object, fileUploadService.Object);
		var file = CreateFormFile(new byte[10], "image/png", "avatar.png");

		await controller.UploadAvatar(file);

		fileUploadService.Verify(service => service.DeleteAsync(previousUrl), Times.Once);
	}

	[Fact]
	public async Task DeleteAvatar_ReturnsNoContent_WhenNoAvatar()
	{
		var userId = Guid.NewGuid();
		var options = new FileUploadOptions();
		var userService = new Mock<IUserService>();
		var fileUploadService = new Mock<IFileUploadService>();

		userService
			.Setup(service => service.GetProfileAsync(userId))
			.ReturnsAsync(new UserDto { Id = userId, AvatarUrl = "" });

		var controller = CreateController(userId, options, userService.Object, fileUploadService.Object);

		var result = await controller.DeleteAvatar();

		result.Should().BeOfType<NoContentResult>();
		userService.Verify(service => service.ClearAvatarAsync(It.IsAny<Guid>()), Times.Never);
		fileUploadService.Verify(service => service.DeleteAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task DeleteAvatar_ClearsAvatarAndDeletesFile()
	{
		var userId = Guid.NewGuid();
		var options = new FileUploadOptions();
		var userService = new Mock<IUserService>();
		var fileUploadService = new Mock<IFileUploadService>();
		var avatarUrl = "http://localhost/uploads/avatars/old.png";

		userService
			.Setup(service => service.GetProfileAsync(userId))
			.ReturnsAsync(new UserDto { Id = userId, AvatarUrl = avatarUrl });

		userService
			.Setup(service => service.ClearAvatarAsync(userId))
			.ReturnsAsync(new UserDto { Id = userId, AvatarUrl = "" });

		var controller = CreateController(userId, options, userService.Object, fileUploadService.Object);

		var result = await controller.DeleteAvatar();

		result.Should().BeOfType<NoContentResult>();
		userService.Verify(service => service.ClearAvatarAsync(userId), Times.Once);
		fileUploadService.Verify(service => service.DeleteAsync(avatarUrl), Times.Once);
	}

	private static UsersController CreateController(
		Guid userId,
		FileUploadOptions options,
		IUserService? userService = null,
		IFileUploadService? fileUploadService = null)
	{
		var userServiceMock = userService ?? new Mock<IUserService>().Object;
		var fileUploadServiceMock = fileUploadService ?? new Mock<IFileUploadService>().Object;
		var avatarMetadataService = new Mock<IAvatarMetadataService>().Object;
		var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UsersController>>().Object;

		var controller = new UsersController(
			userServiceMock,
			fileUploadServiceMock,
			Options.Create(options),
			avatarMetadataService,
			logger);

		var httpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, userId.ToString())
			}, "Test"))
		};

		httpContext.Request.Scheme = "http";
		httpContext.Request.Host = new HostString("localhost");

		controller.ControllerContext = new ControllerContext
		{
			HttpContext = httpContext
		};

		return controller;
	}

	private static IFormFile CreateFormFile(byte[] data, string contentType, string fileName)
	{
		var stream = new MemoryStream(data);
		return new FormFile(stream, 0, stream.Length, "avatar", fileName)
		{
			Headers = new HeaderDictionary(),
			ContentType = contentType
		};
	}
}
