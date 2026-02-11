// Service applicatif Identity: ApiKeyService
using System.Security.Cryptography;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Exceptions;
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using DotnetNiger.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Application.Services;

// Service de gestion des cles API.
public class ApiKeyService : IApiKeyService
{
	private readonly DotnetNigerIdentityDbContext _dbContext;

	public ApiKeyService(DotnetNigerIdentityDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<ApiKeySecretDto> CreateAsync(Guid userId, CreateApiKeyRequest request)
	{
		var name = request.Name?.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new IdentityException("Name is required.", 400);
		}

		if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow)
		{
			throw new IdentityException("ExpiresAt must be in the future.", 400);
		}

		var rawKey = GenerateKey();
		var hashedKey = ApiKeyHasher.Hash(rawKey);

		var apiKey = new ApiKey
		{
			UserId = userId,
			Name = name,
			Key = hashedKey,
			ExpiresAt = request.ExpiresAt,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		_dbContext.ApiKeys.Add(apiKey);
		await _dbContext.SaveChangesAsync();

		return new ApiKeySecretDto
		{
			Id = apiKey.Id,
			Name = apiKey.Name,
			Key = rawKey,
			IsActive = apiKey.IsActive,
			CreatedAt = apiKey.CreatedAt,
			ExpiresAt = apiKey.ExpiresAt
		};
	}

	public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid userId)
	{
		return await _dbContext.ApiKeys
			.AsNoTracking()
			.Where(key => key.UserId == userId)
			.OrderByDescending(key => key.CreatedAt)
			.Select(key => new ApiKeyDto
			{
				Id = key.Id,
				Name = key.Name,
				IsActive = key.IsActive,
				CreatedAt = key.CreatedAt,
				LastUsedAt = key.LastUsedAt,
				ExpiresAt = key.ExpiresAt
			})
			.ToListAsync();
	}

	public async Task<ApiKeySecretDto> RotateAsync(Guid userId, Guid apiKeyId)
	{
		var apiKey = await _dbContext.ApiKeys
			.FirstOrDefaultAsync(key => key.Id == apiKeyId && key.UserId == userId);
		if (apiKey == null)
		{
			throw new IdentityException("Api key not found.", 404);
		}

		if (!apiKey.IsActive)
		{
			throw new IdentityException("Api key is inactive.", 400);
		}

		var rawKey = GenerateKey();
		apiKey.Key = ApiKeyHasher.Hash(rawKey);
		apiKey.LastUsedAt = null;

		await _dbContext.SaveChangesAsync();

		return new ApiKeySecretDto
		{
			Id = apiKey.Id,
			Name = apiKey.Name,
			Key = rawKey,
			IsActive = apiKey.IsActive,
			CreatedAt = apiKey.CreatedAt,
			ExpiresAt = apiKey.ExpiresAt
		};
	}

	public async Task RevokeAsync(Guid userId, Guid apiKeyId)
	{
		var apiKey = await _dbContext.ApiKeys
			.FirstOrDefaultAsync(key => key.Id == apiKeyId && key.UserId == userId);
		if (apiKey == null)
		{
			throw new IdentityException("Api key not found.", 404);
		}

		apiKey.IsActive = false;
		await _dbContext.SaveChangesAsync();
	}

	public async Task RevokeAllAsync(Guid userId)
	{
		var keys = await _dbContext.ApiKeys
			.Where(key => key.UserId == userId && key.IsActive)
			.ToListAsync();

		if (keys.Count == 0)
		{
			return;
		}

		foreach (var key in keys)
		{
			key.IsActive = false;
		}

		await _dbContext.SaveChangesAsync();
	}

	private static string GenerateKey()
	{
		var bytes = RandomNumberGenerator.GetBytes(32);
		var encoded = WebEncoders.Base64UrlEncode(bytes);
		return $"dnk_{encoded}";
	}

}
