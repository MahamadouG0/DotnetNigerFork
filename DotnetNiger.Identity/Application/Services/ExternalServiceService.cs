using Microsoft.EntityFrameworkCore;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.DTOs;

namespace DotnetNiger.Identity.Application.Services;

public class ExternalServiceService
{
    private readonly IdentityDbContext _db;

    public ExternalServiceService(IdentityDbContext db) => _db = db;

    public async Task<ExternalServiceResponse> RegisterAsync(
        Guid tenantId, Guid apiKeyId, RegisterExternalServiceRequest request)
    {
        var slugExists = await _db.ExternalServices.AnyAsync(s => s.Slug == request.Slug);
        if (slugExists)
            throw new InvalidOperationException($"Slug '{request.Slug}' is already taken");

        var service = new ExternalService
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ApiKeyId = apiKeyId,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            BaseUrl = request.BaseUrl.TrimEnd('/'),
            HealthEndpoint = string.IsNullOrWhiteSpace(request.HealthEndpoint) ? "/health" : request.HealthEndpoint,
            Status = ExternalServiceStatus.Active,
        };

        _db.ExternalServices.Add(service);
        await _db.SaveChangesAsync();

        return MapToResponse(service);
    }

    public async Task<PaginatedResponse<ExternalServiceResponse>> GetByTenantAsync(Guid tenantId, PaginationQuery pagination)
    {
        var query = _db.ExternalServices.Where(s => s.TenantId == tenantId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((pagination.EnsurePage - 1) * pagination.EnsurePageSize)
            .Take(pagination.EnsurePageSize)
            .ToListAsync();

        return new PaginatedResponse<ExternalServiceResponse>(
            items.Select(MapToResponse).ToList(), totalCount, pagination.EnsurePage, pagination.EnsurePageSize);
    }

    public async Task<ExternalServiceResponse> GetByIdAsync(Guid tenantId, Guid serviceId)
    {
        var service = await _db.ExternalServices
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.TenantId == tenantId)
            ?? throw new KeyNotFoundException("External service not found");

        return MapToResponse(service);
    }

    public async Task<ServiceLookupResult?> ResolveSlugAsync(string slug)
    {
        var service = await _db.ExternalServices
            .FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive && s.Status == ExternalServiceStatus.Active);

        return service == null ? null : new ServiceLookupResult(service.BaseUrl);
    }

    public async Task<ExternalServiceResponse> UpdateAsync(
        Guid tenantId, Guid serviceId, UpdateExternalServiceRequest request)
    {
        var service = await _db.ExternalServices
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.TenantId == tenantId)
            ?? throw new KeyNotFoundException("External service not found");

        if (request.BaseUrl != null)
            service.BaseUrl = request.BaseUrl.TrimEnd('/');
        if (request.Description != null)
            service.Description = request.Description;
        if (request.HealthEndpoint != null)
            service.HealthEndpoint = request.HealthEndpoint;

        service.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToResponse(service);
    }

    public async Task<List<ExternalServiceResponse>> GetAllActiveAsync()
    {
        var services = await _db.ExternalServices
            .Where(s => s.IsActive && s.Status == ExternalServiceStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return services.Select(MapToResponse).ToList();
    }

    public async Task UpdateHealthStatusAsync(Guid serviceId, bool isHealthy)
    {
        var service = await _db.ExternalServices.FindAsync(serviceId);
        if (service == null) return;

        service.LastHealthCheckAt = DateTime.UtcNow;

        if (isHealthy)
        {
            service.HealthCheckFailures = 0;
            service.Status = ExternalServiceStatus.Active;
        }
        else
        {
            service.HealthCheckFailures++;
            if (service.HealthCheckFailures >= 3)
                service.Status = ExternalServiceStatus.Suspended;
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid tenantId, Guid serviceId)
    {
        var service = await _db.ExternalServices
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.TenantId == tenantId)
            ?? throw new KeyNotFoundException("External service not found");

        _db.ExternalServices.Remove(service);
        await _db.SaveChangesAsync();
    }

    private static ExternalServiceResponse MapToResponse(ExternalService s)
    {
        return new ExternalServiceResponse(
            s.Id, s.TenantId, s.Name, s.Slug, s.Description,
            s.BaseUrl, s.HealthEndpoint, s.IsActive, s.Status,
            s.LastHealthCheckAt, s.HealthCheckFailures,
            s.CreatedAt, s.UpdatedAt);
    }
}
