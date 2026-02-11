// Service applicatif Identity: AdminService
using System.Security.Claims;
using System.Security.Cryptography;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Exceptions;
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using DotnetNiger.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Application.Services;

// Logique d'administration des utilisateurs.
public class AdminService : IAdminService
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly DotnetNigerIdentityDbContext _dbContext;
	private readonly IHttpContextAccessor _httpContextAccessor;

	public AdminService(
		UserManager<ApplicationUser> userManager,
		DotnetNigerIdentityDbContext dbContext,
		IHttpContextAccessor httpContextAccessor)
	{
		_userManager = userManager;
		_dbContext = dbContext;
		_httpContextAccessor = httpContextAccessor;
	}

	public async Task<PaginatedDto<UserSummaryDto>> GetUsersAsync(
		string? search,
		bool? isActive,
		bool? emailConfirmed,
		string? role,
		DateTime? createdFrom,
		DateTime? createdTo,
		string? sortBy,
		string? sortDirection,
		int skip,
		int take)
	{
		var query = _userManager.Users.AsNoTracking();
		var term = search?.Trim();
		if (!string.IsNullOrWhiteSpace(term))
		{
			query = query.Where(user =>
				EF.Functions.Like(user.UserName ?? string.Empty, $"%{term}%") ||
				EF.Functions.Like(user.Email ?? string.Empty, $"%{term}%") ||
				EF.Functions.Like(user.FullName ?? string.Empty, $"%{term}%"));
		}

		if (isActive.HasValue)
		{
			query = query.Where(user => user.IsActive == isActive.Value);
		}

		if (emailConfirmed.HasValue)
		{
			query = query.Where(user => user.EmailConfirmed == emailConfirmed.Value);
		}

		if (createdFrom.HasValue)
		{
			query = query.Where(user => user.CreatedAt >= createdFrom.Value);
		}

		if (createdTo.HasValue)
		{
			query = query.Where(user => user.CreatedAt <= createdTo.Value);
		}

		if (!string.IsNullOrWhiteSpace(role))
		{
			var usersInRole = await _userManager.GetUsersInRoleAsync(role);
			var userIds = usersInRole.Select(user => user.Id).ToList();
			query = query.Where(user => userIds.Contains(user.Id));
		}

		query = ApplySorting(query, sortBy, sortDirection);

		var total = await query.CountAsync();
		var users = await query
			.Skip(skip)
			.Take(take)
			.Select(user => new UserSummaryDto
			{
				Id = user.Id,
				Username = user.UserName ?? string.Empty,
				Email = user.Email ?? string.Empty,
				FullName = user.FullName,
				IsActive = user.IsActive,
				EmailConfirmed = user.EmailConfirmed,
				CreatedAt = user.CreatedAt,
				LastLoginAt = user.LastLoginAt
			})
			.ToListAsync();

		return new PaginatedDto<UserSummaryDto>
		{
			Items = users,
			TotalCount = total,
			Skip = skip,
			Take = take
		};
	}

	private static IQueryable<ApplicationUser> ApplySorting(
		IQueryable<ApplicationUser> query,
		string? sortBy,
		string? sortDirection)
	{
		var direction = sortDirection?.Trim().ToLowerInvariant();
		var ascending = direction == "asc";
		var key = sortBy?.Trim().ToLowerInvariant();

		return key switch
		{
			"username" => ascending ? query.OrderBy(user => user.UserName) : query.OrderByDescending(user => user.UserName),
			"email" => ascending ? query.OrderBy(user => user.Email) : query.OrderByDescending(user => user.Email),
			"lastlogin" => ascending ? query.OrderBy(user => user.LastLoginAt) : query.OrderByDescending(user => user.LastLoginAt),
			"createdat" => ascending ? query.OrderBy(user => user.CreatedAt) : query.OrderByDescending(user => user.CreatedAt),
			_ => query.OrderByDescending(user => user.CreatedAt)
		};
	}

	public async Task SetUserActiveAsync(Guid userId, bool isActive)
	{
		var user = await _userManager.FindByIdAsync(userId.ToString());
		if (user == null)
		{
			throw new IdentityException("User not found.", 404);
		}

		user.IsActive = isActive;
		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}

		await LogAdminActionAsync(
			"user.status.update",
			"user",
			user.Id.ToString(),
			$"isActive={isActive}");
	}

	public async Task<PaginatedDto<ApiKeyAuditDto>> GetApiKeysAsync(
		string? search,
		Guid? userId,
		bool? isActive,
		bool? expired,
		DateTime? createdFrom,
		DateTime? createdTo,
		DateTime? lastUsedFrom,
		DateTime? lastUsedTo,
		string? sortBy,
		string? sortDirection,
		int skip,
		int take)
	{
		// Audit centralise des cles API avec filtres admin.
		var query = _dbContext.ApiKeys
			.AsNoTracking()
			.Include(key => key.User)
			.AsQueryable();

		var term = search?.Trim();
		if (!string.IsNullOrWhiteSpace(term))
		{
			query = query.Where(key =>
				EF.Functions.Like(key.Name, $"%{term}%") ||
				EF.Functions.Like(key.User.UserName ?? string.Empty, $"%{term}%") ||
				EF.Functions.Like(key.User.Email ?? string.Empty, $"%{term}%"));
		}

		if (userId.HasValue)
		{
			query = query.Where(key => key.UserId == userId.Value);
		}

		if (isActive.HasValue)
		{
			query = query.Where(key => key.IsActive == isActive.Value);
		}

		if (expired.HasValue)
		{
			if (expired.Value)
			{
				query = query.Where(key => key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTime.UtcNow);
			}
			else
			{
				query = query.Where(key => !key.ExpiresAt.HasValue || key.ExpiresAt.Value > DateTime.UtcNow);
			}
		}

		if (createdFrom.HasValue)
		{
			query = query.Where(key => key.CreatedAt >= createdFrom.Value);
		}

		if (createdTo.HasValue)
		{
			query = query.Where(key => key.CreatedAt <= createdTo.Value);
		}

		if (lastUsedFrom.HasValue)
		{
			query = query.Where(key => key.LastUsedAt.HasValue && key.LastUsedAt.Value >= lastUsedFrom.Value);
		}

		if (lastUsedTo.HasValue)
		{
			query = query.Where(key => key.LastUsedAt.HasValue && key.LastUsedAt.Value <= lastUsedTo.Value);
		}

		query = ApplyApiKeySorting(query, sortBy, sortDirection);

		var total = await query.CountAsync();
		var items = await query
			.Skip(skip)
			.Take(take)
			.Select(key => new ApiKeyAuditDto
			{
				Id = key.Id,
				Name = key.Name,
				IsActive = key.IsActive,
				CreatedAt = key.CreatedAt,
				LastUsedAt = key.LastUsedAt,
				ExpiresAt = key.ExpiresAt,
				IsExpired = key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTime.UtcNow,
				UserId = key.UserId,
				Username = key.User.UserName ?? string.Empty,
				Email = key.User.Email ?? string.Empty
			})
			.ToListAsync();

		return new PaginatedDto<ApiKeyAuditDto>
		{
			Items = items,
			TotalCount = total,
			Skip = skip,
			Take = take
		};
	}

	private static IQueryable<ApiKey> ApplyApiKeySorting(
		IQueryable<ApiKey> query,
		string? sortBy,
		string? sortDirection)
	{
		var direction = sortDirection?.Trim().ToLowerInvariant();
		var ascending = direction == "asc";
		var key = sortBy?.Trim().ToLowerInvariant();

		return key switch
		{
			"name" => ascending ? query.OrderBy(item => item.Name) : query.OrderByDescending(item => item.Name),
			"lastused" => ascending ? query.OrderBy(item => item.LastUsedAt) : query.OrderByDescending(item => item.LastUsedAt),
			"expiresat" => ascending ? query.OrderBy(item => item.ExpiresAt) : query.OrderByDescending(item => item.ExpiresAt),
			"username" => ascending ? query.OrderBy(item => item.User.UserName) : query.OrderByDescending(item => item.User.UserName),
			"createdat" => ascending ? query.OrderBy(item => item.CreatedAt) : query.OrderByDescending(item => item.CreatedAt),
			_ => query.OrderByDescending(item => item.CreatedAt)
		};
	}

	public async Task<ApiKeySecretDto> RotateApiKeyAsync(Guid apiKeyId)
	{
		var apiKey = await _dbContext.ApiKeys.FirstOrDefaultAsync(key => key.Id == apiKeyId);
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
		await LogAdminActionAsync(
			"api_key.rotate",
			"api_key",
			apiKey.Id.ToString(),
			$"userId={apiKey.UserId}");

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

	public async Task RevokeApiKeyAsync(Guid apiKeyId)
	{
		var apiKey = await _dbContext.ApiKeys.FirstOrDefaultAsync(key => key.Id == apiKeyId);
		if (apiKey == null)
		{
			throw new IdentityException("Api key not found.", 404);
		}

		apiKey.IsActive = false;
		await _dbContext.SaveChangesAsync();
		await LogAdminActionAsync(
			"api_key.revoke",
			"api_key",
			apiKey.Id.ToString(),
			$"userId={apiKey.UserId}");
	}

	public async Task RevokeUserApiKeysAsync(Guid userId)
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
		await LogAdminActionAsync(
			"api_key.revoke_all",
			"user",
			userId.ToString(),
			$"count={keys.Count}");
	}

	public async Task<PaginatedDto<AdminAuditLogDto>> GetAdminAuditLogsAsync(
		Guid? adminUserId,
		string? action,
		string? targetType,
		DateTime? createdFrom,
		DateTime? createdTo,
		int skip,
		int take)
	{
		// Lecture du journal d'audit pour supervision et conformite.
		var query = _dbContext.AdminActionLogs
			.AsNoTracking()
			.Include(log => log.AdminUser)
			.AsQueryable();

		if (adminUserId.HasValue)
		{
			query = query.Where(log => log.AdminUserId == adminUserId.Value);
		}

		if (!string.IsNullOrWhiteSpace(action))
		{
			var term = action.Trim();
			query = query.Where(log => EF.Functions.Like(log.Action, $"%{term}%"));
		}

		if (!string.IsNullOrWhiteSpace(targetType))
		{
			var term = targetType.Trim();
			query = query.Where(log => EF.Functions.Like(log.TargetType, $"%{term}%"));
		}

		if (createdFrom.HasValue)
		{
			query = query.Where(log => log.CreatedAt >= createdFrom.Value);
		}

		if (createdTo.HasValue)
		{
			query = query.Where(log => log.CreatedAt <= createdTo.Value);
		}

		var total = await query.CountAsync();
		var items = await query
			.OrderByDescending(log => log.CreatedAt)
			.Skip(skip)
			.Take(take)
			.Select(log => new AdminAuditLogDto
			{
				Id = log.Id,
				AdminUserId = log.AdminUserId,
				AdminUsername = log.AdminUser.UserName ?? string.Empty,
				AdminEmail = log.AdminUser.Email ?? string.Empty,
				Action = log.Action,
				TargetType = log.TargetType,
				TargetId = log.TargetId,
				Details = log.Details,
				IpAddress = log.IpAddress,
				UserAgent = log.UserAgent,
				CreatedAt = log.CreatedAt
			})
			.ToListAsync();

		return new PaginatedDto<AdminAuditLogDto>
		{
			Items = items,
			TotalCount = total,
			Skip = skip,
			Take = take
		};
	}

	private static string GenerateKey()
	{
		var bytes = RandomNumberGenerator.GetBytes(32);
		var encoded = WebEncoders.Base64UrlEncode(bytes);
		return $"dnk_{encoded}";
	}

	private async Task LogAdminActionAsync(string action, string targetType, string targetId, string details)
	{
		// Enregistre l'action admin courante avec contexte HTTP.
		var adminUserId = GetAdminUserId();
		if (!adminUserId.HasValue)
		{
			return;
		}

		var context = _httpContextAccessor.HttpContext;
		var ip = context?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
		var userAgent = context?.Request.Headers.UserAgent.ToString() ?? string.Empty;

		var log = new AdminActionLog
		{
			AdminUserId = adminUserId.Value,
			Action = action,
			TargetType = targetType,
			TargetId = targetId,
			Details = details,
			IpAddress = ip,
			UserAgent = userAgent,
			CreatedAt = DateTime.UtcNow
		};

		_dbContext.AdminActionLogs.Add(log);
		await _dbContext.SaveChangesAsync();
	}

	private Guid? GetAdminUserId()
	{
		var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		return Guid.TryParse(value, out var userId) ? userId : null;
	}
}
