using DotnetNiger.Community.Application.DTOs;

namespace DotnetNiger.Community.Application.Services;

public interface IResourceService
{
    Task<PaginatedResponse<ResourceResponse>> GetAllAsync(string? resourceType, string? level, string? query, string? tag, Guid? categoryId, int page = 1, int pageSize = 10);
    Task<ResourceResponse?> GetByIdAsync(Guid id);
    Task<ResourceResponse> CreateAsync(CreateResourceRequest request, Guid userId);
    Task<ResourceResponse?> UpdateAsync(Guid id, CreateResourceRequest request, Guid userId, bool isAdmin);
    Task<bool> DeleteAsync(Guid id, Guid userId, bool isAdmin);
    Task<ResourceResponse?> IncrementViewCountAsync(Guid id);
}
