// Service applicatif Identity: UserService
using System.ComponentModel.DataAnnotations;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Exceptions;
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Application.Validators;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Application.Services;

// Service de gestion du profil et du compte.
public class UserService : IUserService
{
	// Lecture du profil utilisateur.
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly DotnetNigerIdentityDbContext _dbContext;

	public UserService(UserManager<ApplicationUser> userManager, DotnetNigerIdentityDbContext dbContext)
	{
		_userManager = userManager;
		_dbContext = dbContext;
	}

	public async Task<UserDto> GetProfileAsync(Guid userId)
	{
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
		{
			throw new UserNotFoundException();
		}

		return await MapUserAsync(user);
	}

	public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
	{
		UpdateProfileRequestValidator.ValidateAndThrow(request);
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
		{
			throw new UserNotFoundException();
		}

		user.FullName = request.FullName ?? string.Empty;
		user.Bio = request.Bio ?? string.Empty;
		user.AvatarUrl = request.AvatarUrl ?? string.Empty;
		user.Country = request.Country ?? string.Empty;
		user.City = request.City ?? string.Empty;

		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}

		return await MapUserAsync(user);
	}

	public async Task<UserDto> UpdateAvatarAsync(Guid userId, string avatarUrl)
	{
		if (string.IsNullOrWhiteSpace(avatarUrl))
		{
			throw new IdentityException("AvatarUrl is required.", 400);
		}

		if (!new UrlAttribute().IsValid(avatarUrl))
		{
			throw new IdentityException("AvatarUrl is invalid.", 400);
		}

		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
		{
			throw new UserNotFoundException();
		}

		user.AvatarUrl = avatarUrl;
		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}

		return await MapUserAsync(user);
	}

	public async Task<UserDto> ClearAvatarAsync(Guid userId)
	{
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
		{
			throw new UserNotFoundException();
		}

		user.AvatarUrl = string.Empty;
		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}

		return await MapUserAsync(user);
	}

	public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
	{
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
		{
			throw new UserNotFoundException();
		}

		var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}
	}

	public async Task ChangeEmailAsync(Guid userId, ChangeEmailRequest request)
	{
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
		{
			throw new UserNotFoundException();
		}

		var passwordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
		if (!passwordValid)
		{
			throw new InvalidCredentialsException();
		}

		var newEmail = request.NewEmail?.Trim();
		if (string.IsNullOrWhiteSpace(newEmail))
		{
			throw new IdentityException("Email is required.", 400);
		}

		var existing = await _userManager.FindByEmailAsync(newEmail);
		if (existing != null && existing.Id != user.Id)
		{
			throw new IdentityException("Email already in use.", 409);
		}

		var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
		var result = await _userManager.ChangeEmailAsync(user, newEmail, token);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}
	}

	private async Task<UserDto> MapUserAsync(ApplicationUser user)
	{
		var roles = await _userManager.GetRolesAsync(user);
		var socialLinks = await _dbContext.SocialLinks
			.Where(link => link.UserId == user.Id)
			.Select(link => new SocialLinkDto
			{
				Id = link.Id,
				Platform = link.Platform,
				Url = link.Url
			})
			.ToListAsync();

		return new UserDto
		{
			Id = user.Id,
			Username = user.UserName ?? string.Empty,
			Email = user.Email ?? string.Empty,
			FullName = user.FullName,
			Bio = user.Bio,
			AvatarUrl = user.AvatarUrl,
			Country = user.Country ?? string.Empty,
			City = user.City ?? string.Empty,
			IsActive = user.IsActive,
			CreatedAt = user.CreatedAt,
			LastLoginAt = user.LastLoginAt,
			Roles = roles.ToList(),
			SocialLinks = socialLinks
		};
	}
}
