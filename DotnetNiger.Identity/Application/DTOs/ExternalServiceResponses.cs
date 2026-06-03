using DotnetNiger.Identity.Domain.Entities;

namespace DotnetNiger.Identity.Application.DTOs;

public record ExternalServiceResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string Slug,
    string? Description,
    string BaseUrl,
    string HealthEndpoint,
    bool IsActive,
    ExternalServiceStatus Status,
    DateTime? LastHealthCheckAt,
    int HealthCheckFailures,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ServiceLookupResult(string BaseUrl);
