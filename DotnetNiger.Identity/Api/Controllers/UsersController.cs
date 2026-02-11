// Controleur API Identity: UsersController
using System.Security.Claims;
using Asp.Versioning;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Exceptions;
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Infrastructure.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
// Endpoints pour le profil et les operations du compte.
public class UsersController : ControllerBase
{
	// Endpoints proteges pour le profil utilisateur.
	private readonly IUserService _userService;
	private readonly IFileUploadService _fileUploadService;
	private readonly FileUploadOptions _fileUploadOptions;
	private readonly IAvatarMetadataService _avatarMetadataService;
	private readonly ILogger<UsersController> _logger;

	public UsersController(
		IUserService userService,
		IFileUploadService fileUploadService,
		IOptions<FileUploadOptions> fileUploadOptions,
		IAvatarMetadataService avatarMetadataService,
		ILogger<UsersController> logger)
	{
		_userService = userService;
		_fileUploadService = fileUploadService;
		_fileUploadOptions = fileUploadOptions.Value;
		_avatarMetadataService = avatarMetadataService;
		_logger = logger;
	}

	[HttpGet("me")]
	public async Task<ActionResult<UserDto>> Me()
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		var profile = await _userService.GetProfileAsync(userId.Value);
		return Ok(profile);
	}

	[HttpPut("me")]
	public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateProfileRequest request)
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		var profile = await _userService.UpdateProfileAsync(userId.Value, request);
		return Ok(profile);
	}

	[HttpGet("me/avatar")]
	[ProducesResponseType(typeof(AvatarInfoDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<AvatarInfoDto>> GetAvatar()
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		var profile = await _userService.GetProfileAsync(userId.Value);
		var metadata = await _avatarMetadataService.GetMetadataAsync(profile.AvatarUrl);
		return Ok(metadata);
	}

	[HttpPost("me/change-password")]
	public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		await _userService.ChangePasswordAsync(userId.Value, request);
		return NoContent();
	}

	[HttpPost("me/change-email")]
	public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		await _userService.ChangeEmailAsync(userId.Value, request);
		return NoContent();
	}

	[HttpPost("me/avatar")]
	[Consumes("multipart/form-data")]
	[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<UserDto>> UploadAvatar([FromForm] IFormFile avatar)
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		var currentProfile = await _userService.GetProfileAsync(userId.Value);
		var previousAvatarUrl = currentProfile.AvatarUrl;

		if (avatar == null || avatar.Length <= 0)
		{
			throw new IdentityException("Avatar file is required.", 400);
		}

		if (avatar.Length > _fileUploadOptions.MaxAvatarBytes)
		{
			throw new IdentityException("Avatar file is too large.", 400);
		}

		if (!_fileUploadOptions.AllowedAvatarContentTypes
			.Contains(avatar.ContentType, StringComparer.OrdinalIgnoreCase))
		{
			throw new IdentityException("Avatar content type is not allowed.", 400);
		}

		var extension = GetAvatarExtension(avatar.ContentType, avatar.FileName);
		if (string.IsNullOrWhiteSpace(extension))
		{
			throw new IdentityException("Avatar extension is invalid.", 400);
		}

		if (!_fileUploadOptions.AllowedAvatarExtensions
			.Contains(extension, StringComparer.OrdinalIgnoreCase))
		{
			throw new IdentityException("Avatar extension is not allowed.", 400);
		}
		var fileName = $"avatars/{userId}/{Guid.NewGuid():N}{extension}";

		await using var stream = avatar.OpenReadStream();
		var relativeUrl = await _fileUploadService.UploadAsync(stream, fileName, avatar.ContentType);
		var absoluteUrl = BuildAbsoluteUrl(relativeUrl);
		var profile = await _userService.UpdateAvatarAsync(userId.Value, absoluteUrl);

		if (!string.IsNullOrWhiteSpace(previousAvatarUrl) &&
			!string.Equals(previousAvatarUrl, absoluteUrl, StringComparison.OrdinalIgnoreCase))
		{
			await TryDeleteAsync(previousAvatarUrl, "upload");
		}

		return Ok(profile);
	}

	[HttpDelete("me/avatar")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> DeleteAvatar()
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		var currentProfile = await _userService.GetProfileAsync(userId.Value);
		if (string.IsNullOrWhiteSpace(currentProfile.AvatarUrl))
		{
			return NoContent();
		}

		await _userService.ClearAvatarAsync(userId.Value);
		await TryDeleteAsync(currentProfile.AvatarUrl, "delete");
		return NoContent();
	}

	private Guid? GetUserId()
	{
		var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrWhiteSpace(userIdValue))
		{
			return null;
		}

		return Guid.TryParse(userIdValue, out var userId) ? userId : null;
	}

	private string BuildAbsoluteUrl(string url)
	{
		if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
		{
			return absolute.ToString();
		}

		var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
		var relative = url.StartsWith('/') ? url : "/" + url;
		return $"{baseUrl}{relative}";
	}

	private static string GetAvatarExtension(string contentType, string fileName)
	{
		var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
		if (!string.IsNullOrWhiteSpace(fileExtension))
		{
			return fileExtension;
		}

		var extension = contentType.ToLowerInvariant() switch
		{
			"image/jpeg" => ".jpg",
			"image/png" => ".png",
			"image/webp" => ".webp",
			_ => string.Empty
		};

		return extension;
	}

	private async Task TryDeleteAsync(string fileUrl, string context)
	{
		try
		{
			await _fileUploadService.DeleteAsync(fileUrl);
		}
		catch (Exception exception)
		{
			_logger.LogWarning(exception, "Avatar cleanup failed on {Context}", context);
		}
	}
}
