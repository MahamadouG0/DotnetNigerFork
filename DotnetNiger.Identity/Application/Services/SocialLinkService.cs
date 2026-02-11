// Service applicatif Identity: SocialLinkService
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

// Gestion des liens sociaux associes aux utilisateurs.
public class SocialLinkService : ISocialLinkService
{
	// Gestion des liens sociaux utilisateur.
	private readonly DotnetNigerIdentityDbContext _dbContext;
	private readonly UserManager<ApplicationUser> _userManager;

	public SocialLinkService(DotnetNigerIdentityDbContext dbContext, UserManager<ApplicationUser> userManager)
	{
		_dbContext = dbContext;
		_userManager = userManager;
	}

	public async Task<IReadOnlyList<SocialLinkDto>> GetForUserAsync(Guid userId)
	{
		var user = await _userManager.FindByIdAsync(userId.ToString());
		if (user == null)
		{
			throw new UserNotFoundException();
		}

		return await _dbContext.SocialLinks
			.Where(link => link.UserId == user.Id)
			.OrderByDescending(link => link.CreatedAt)
			.Select(link => new SocialLinkDto
			{
				Id = link.Id,
				Platform = link.Platform,
				Url = link.Url
			})
			.ToListAsync();
	}

	public async Task<SocialLinkDto> AddAsync(Guid userId, AddSocialLinkRequest request)
	{
		AddSocialLinkValidator.ValidateAndThrow(request);
		var user = await _userManager.FindByIdAsync(userId.ToString());
		if (user == null)
		{
			throw new UserNotFoundException();
		}

		var link = new SocialLink
		{
			UserId = user.Id,
			Platform = request.Platform?.Trim() ?? string.Empty,
			Url = request.Url?.Trim() ?? string.Empty
		};

		_dbContext.SocialLinks.Add(link);
		await _dbContext.SaveChangesAsync();

		return new SocialLinkDto
		{
			Id = link.Id,
			Platform = link.Platform,
			Url = link.Url
		};
	}

	public async Task DeleteAsync(Guid userId, Guid linkId)
	{
		var user = await _userManager.FindByIdAsync(userId.ToString());
		if (user == null)
		{
			throw new UserNotFoundException();
		}

		var link = await _dbContext.SocialLinks
			.FirstOrDefaultAsync(item => item.Id == linkId && item.UserId == user.Id);
		if (link == null)
		{
			throw new IdentityException("Social link not found.", 404);
		}

		_dbContext.SocialLinks.Remove(link);
		await _dbContext.SaveChangesAsync();
	}
}
