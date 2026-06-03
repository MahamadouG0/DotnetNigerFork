using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Application.Services;

/// <summary>
/// Service for logging administrative actions to an audit trail.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Logs an administrative action.
    /// </summary>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="entityType">The type of entity acted upon.</param>
    /// <param name="entityId">The ID of the entity acted upon.</param>
    /// <param name="action">The action performed (e.g., Create, Update, Delete).</param>
    /// <param name="description">Optional description of the changes made.</param>
    /// <param name="ipAddress">The IP address from which the action originated.</param>
    /// <param name="tenantId">The tenant ID (optional, will be resolved from HttpContext if not provided).</param>
    /// <returns>The created audit log entry.</returns>
    Task<AuditLog> LogAsync(
        Guid userId,
        string entityType,
        Guid entityId,
        string action,
        string? description = null,
        string? ipAddress = null,
        Guid? tenantId = null);
}

public class AuditLogService : IAuditLogService
{
    private readonly IdentityDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantContext _tenantContext;

    public AuditLogService(
        IdentityDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        TenantContext tenantContext)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    public async Task<AuditLog> LogAsync(
        Guid userId,
        string entityType,
        Guid entityId,
        string action,
        string? description = null,
        string? ipAddress = null,
        Guid? tenantId = null)
    {
        // Use provided tenantId or resolve from HttpContext
        var resolvedTenantId = tenantId ?? _tenantContext.TenantId;
        
        // Use provided IP address or resolve from HttpContext
        var resolvedIpAddress = ipAddress ?? 
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var auditLog = new AuditLog
        {
            TenantId = resolvedTenantId,
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Description = description,
            IpAddress = resolvedIpAddress,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync();

        return auditLog;
    }
}