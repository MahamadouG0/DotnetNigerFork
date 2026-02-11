// Service applicatif Identity: AuthService
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Exceptions;
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Application.Validators;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using DotnetNiger.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DotnetNiger.Identity.Application.Services;

// Service d'authentification et de gestion des tokens.
public class AuthService : IAuthService
{
	// Logique de login/inscription.
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly DotnetNigerIdentityDbContext _dbContext;
	private readonly JwtTokenGenerator _jwtTokenGenerator;
	private readonly RefreshTokenGenerator _refreshTokenGenerator;
	private readonly JwtOptions _jwtOptions;
	private readonly IEmailService _emailService;
	private readonly ILoginHistoryService _loginHistoryService;

	public AuthService(
		UserManager<ApplicationUser> userManager,
		DotnetNigerIdentityDbContext dbContext,
		JwtTokenGenerator jwtTokenGenerator,
		RefreshTokenGenerator refreshTokenGenerator,
		IOptions<JwtOptions> jwtOptions,
		IEmailService emailService,
		ILoginHistoryService loginHistoryService)
	{
		_userManager = userManager;
		_dbContext = dbContext;
		_jwtTokenGenerator = jwtTokenGenerator;
		_refreshTokenGenerator = refreshTokenGenerator;
		_jwtOptions = jwtOptions.Value;
		_emailService = emailService;
		_loginHistoryService = loginHistoryService;
	}

	public async Task<AuthDto> RegisterAsync(RegisterRequest request)
	{
		RegisterRequestValidator.ValidateAndThrow(request);
		var existingByEmail = await _userManager.FindByEmailAsync(request.Email);
		if (existingByEmail != null)
		{
			throw new UserAlreadyExistsException("Email already in use.");
		}

		var existingByUsername = await _userManager.FindByNameAsync(request.Username);
		if (existingByUsername != null)
		{
			throw new UserAlreadyExistsException("Username already in use.");
		}

		var user = new ApplicationUser
		{
			UserName = request.Username,
			Email = request.Email,
			FullName = request.FullName,
			Country = request.Country,
			City = request.City,
			IsActive = true
		};

		var result = await _userManager.CreateAsync(user, request.Password);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}

		var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
		await _emailService.SendAsync(user.Email ?? string.Empty, "Verify email", $"Your verification token: {confirmationToken}");

		var tokenDto = await CreateTokenAsync(user);
		var userDto = await MapUserAsync(user);

		return new AuthDto
		{
			Success = true,
			Message = "Registration successful. Please verify your email.",
			User = userDto,
			Token = tokenDto
		};
	}

	public async Task<AuthDto> LoginAsync(LoginRequest request)
	{
		LoginRequestValidator.ValidateAndThrow(request);
		var user = await _userManager.FindByEmailAsync(request.Email);
		if (user == null)
		{
			throw new InvalidCredentialsException();
		}

		if (!user.IsActive)
		{
			await _loginHistoryService.RecordAsync(user.Id, false, "User disabled.");
			throw new IdentityException("User is disabled.", 403);
		}

		if (!user.EmailConfirmed)
		{
			await _loginHistoryService.RecordAsync(user.Id, false, "Email not verified.");
			throw new IdentityException("Email is not verified.", 403);
		}

		var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
		if (!passwordValid)
		{
			await _loginHistoryService.RecordAsync(user.Id, false, "Invalid credentials.");
			throw new InvalidCredentialsException();
		}

		user.LastLoginAt = DateTime.UtcNow;
		await _userManager.UpdateAsync(user);
		await _loginHistoryService.RecordAsync(user.Id, true, string.Empty);

		var tokenDto = await CreateTokenAsync(user);
		var userDto = await MapUserAsync(user);

		return new AuthDto
		{
			Success = true,
			Message = "Login successful.",
			User = userDto,
			Token = tokenDto
		};
	}

	public async Task<string?> RequestEmailVerificationAsync(RequestEmailVerificationRequest request)
	{
		var email = request.Email?.Trim();
		if (string.IsNullOrWhiteSpace(email))
		{
			throw new IdentityException("Email is required.", 400);
		}

		var user = await _userManager.FindByEmailAsync(email);
		if (user == null)
		{
			return null;
		}

		if (user.EmailConfirmed)
		{
			return null;
		}

		var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
		await _emailService.SendAsync(email, "Verify email", $"Your verification token: {token}");
		return token;
	}

	public async Task<string?> RequestPasswordResetAsync(ForgotPasswordRequest request)
	{
		var email = request.Email?.Trim();
		if (string.IsNullOrWhiteSpace(email))
		{
			throw new IdentityException("Email is required.", 400);
		}

		var user = await _userManager.FindByEmailAsync(email);
		if (user == null)
		{
			return null;
		}

		var token = await _userManager.GeneratePasswordResetTokenAsync(user);
		await _emailService.SendAsync(email, "Reset password", $"Your reset token: {token}");
		return token;
	}

	public async Task ResetPasswordAsync(ResetPasswordRequest request)
	{
		ResetPasswordRequestValidator.ValidateAndThrow(request);
		var email = request.Email?.Trim();
		if (string.IsNullOrWhiteSpace(email))
		{
			throw new IdentityException("Email is required.", 400);
		}

		var user = await _userManager.FindByEmailAsync(email);
		if (user == null)
		{
			throw new IdentityException("Invalid reset request.", 400);
		}

		var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}
	}

	public async Task VerifyEmailAsync(VerifyEmailRequest request)
	{
		var email = request.Email?.Trim();
		if (string.IsNullOrWhiteSpace(email))
		{
			throw new IdentityException("Email is required.", 400);
		}

		var user = await _userManager.FindByEmailAsync(email);
		if (user == null)
		{
			throw new IdentityException("Invalid verification request.", 400);
		}

		var result = await _userManager.ConfirmEmailAsync(user, request.Token);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}
	}

	private async Task<TokenDto> CreateTokenAsync(ApplicationUser user)
	{
		var accessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(user);
		var refreshTokenValue = _refreshTokenGenerator.GenerateToken();
		var refreshToken = new RefreshToken
		{
			UserId = user.Id,
			Token = refreshTokenValue,
			ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
		};

		_dbContext.RefreshTokens.Add(refreshToken);
		await _dbContext.SaveChangesAsync();

		return new TokenDto
		{
			AccessToken = accessToken,
			RefreshToken = refreshTokenValue,
			ExpiresIn = _jwtOptions.AccessTokenMinutes * 60,
			TokenType = "Bearer"
		};
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
