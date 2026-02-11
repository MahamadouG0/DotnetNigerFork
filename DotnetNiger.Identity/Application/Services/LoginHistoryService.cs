// Service applicatif Identity: LoginHistoryService
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Application.Services;

// Service de journalisation des connexions.
public class LoginHistoryService : ILoginHistoryService
{
	private readonly DotnetNigerIdentityDbContext _dbContext;
	private readonly IHttpContextAccessor _httpContextAccessor;

	public LoginHistoryService(
		DotnetNigerIdentityDbContext dbContext,
		IHttpContextAccessor httpContextAccessor)
	{
		_dbContext = dbContext;
		_httpContextAccessor = httpContextAccessor;
	}

	public async Task RecordAsync(Guid userId, bool success, string? failureReason)
	{
		var context = _httpContextAccessor.HttpContext;
		var ip = context?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
		var userAgent = context?.Request.Headers.UserAgent.ToString() ?? string.Empty;

		var history = new LoginHistory
		{
			UserId = userId,
			Success = success,
			FailureReason = failureReason ?? string.Empty,
			IpAddress = ip,
			UserAgent = userAgent
		};

		_dbContext.LoginHistories.Add(history);
		await _dbContext.SaveChangesAsync();
	}

	public async Task<PaginatedDto<LoginHistoryDto>> GetUserHistoryAsync(Guid userId, int skip, int take)
	{
		var query = _dbContext.LoginHistories
			.AsNoTracking()
			.Where(history => history.UserId == userId);

		var total = await query.CountAsync();
		var items = await query
			.OrderByDescending(history => history.LoginAt)
			.Skip(skip)
			.Take(take)
			.Select(history => new LoginHistoryDto
			{
				Id = history.Id,
				LoginAt = history.LoginAt,
				IpAddress = history.IpAddress,
				UserAgent = history.UserAgent,
				Success = history.Success,
				FailureReason = history.FailureReason,
				Country = history.Country,
				City = history.City
			})
			.ToListAsync();

		return new PaginatedDto<LoginHistoryDto>
		{
			Items = items,
			TotalCount = total,
			Skip = skip,
			Take = take
		};
	}
}
