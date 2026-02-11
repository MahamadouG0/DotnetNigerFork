// Service applicatif Identity: TokenService
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Exceptions;
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using DotnetNiger.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DotnetNiger.Identity.Application.Services;

public class TokenService : ITokenService
{
	// Rotation et reemission des tokens.
	private readonly DotnetNigerIdentityDbContext _dbContext;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly JwtTokenGenerator _jwtTokenGenerator;
	private readonly RefreshTokenGenerator _refreshTokenGenerator;
	private readonly JwtOptions _jwtOptions;

	public TokenService(
		DotnetNigerIdentityDbContext dbContext,
		UserManager<ApplicationUser> userManager,
		JwtTokenGenerator jwtTokenGenerator,
		RefreshTokenGenerator refreshTokenGenerator,
		IOptions<JwtOptions> jwtOptions)
	{
		_dbContext = dbContext;
		_userManager = userManager;
		_jwtTokenGenerator = jwtTokenGenerator;
		_refreshTokenGenerator = refreshTokenGenerator;
		_jwtOptions = jwtOptions.Value;
	}

	public async Task<AuthDto> RefreshAsync(RefreshTokenRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.RefreshToken))
		{
			throw new IdentityException("Refresh token is required.", 400);
		}

		var storedToken = await _dbContext.RefreshTokens
			.Include(token => token.User)
			.FirstOrDefaultAsync(token => token.Token == request.RefreshToken);

		if (storedToken == null || storedToken.RevokedAt != null)
		{
			throw new InvalidCredentialsException();
		}

		if (storedToken.ExpiresAt <= DateTime.UtcNow)
		{
			throw new TokenExpiredException();
		}

		var user = storedToken.User;
		if (user == null)
		{
			throw new UserNotFoundException();
		}

		// Rotation du refresh token pour limiter la reutilisation.
		storedToken.RevokedAt = DateTime.UtcNow;
		var newRefreshTokenValue = _refreshTokenGenerator.GenerateToken();
		var newRefreshToken = new RefreshToken
		{
			UserId = user.Id,
			Token = newRefreshTokenValue,
			ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
		};

		_dbContext.RefreshTokens.Add(newRefreshToken);
		await _dbContext.SaveChangesAsync();

		var accessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(user);
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

		return new AuthDto
		{
			Success = true,
			Message = "Token refreshed.",
			User = new UserDto
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
			},
			Token = new TokenDto
			{
				AccessToken = accessToken,
				RefreshToken = newRefreshTokenValue,
				ExpiresIn = _jwtOptions.AccessTokenMinutes * 60,
				TokenType = "Bearer"
			}
		};
	}

	public async Task LogoutAsync(Guid userId, RefreshTokenRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.RefreshToken))
		{
			throw new IdentityException("Refresh token is required.", 400);
		}

		var storedToken = await _dbContext.RefreshTokens
			.FirstOrDefaultAsync(token => token.Token == request.RefreshToken);
		if (storedToken == null || storedToken.UserId != userId)
		{
			throw new InvalidCredentialsException();
		}

		if (storedToken.RevokedAt != null)
		{
			return;
		}

		storedToken.RevokedAt = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync();
	}
}
