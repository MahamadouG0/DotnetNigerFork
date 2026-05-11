using DotnetNiger.Community.Application.DTOs;

namespace DotnetNiger.Community.Application.Services;

public interface IResourceService
{
    Task<PaginatedResponse<ResourceResponse>> GetAllAsync(string? resourceType, string? level, string? query, int page = 1, int pageSize = 10);
    Task<ResourceResponse?> GetByIdAsync(Guid id);
    Task<ResourceResponse> CreateAsync(CreateResourceRequest request);
    Task<ResourceResponse?> UpdateAsync(Guid id, CreateResourceRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<ResourceResponse?> IncrementViewCountAsync(Guid id);
}
