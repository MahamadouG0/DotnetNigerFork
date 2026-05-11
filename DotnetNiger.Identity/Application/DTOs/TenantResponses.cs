namespace DotnetNiger.Identity.Application.DTOs;

public record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    bool IsActive,
    DateTime CreatedAt);
