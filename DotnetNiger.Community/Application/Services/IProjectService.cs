using DotnetNiger.Community.Application.DTOs;

namespace DotnetNiger.Community.Application.Services;

public interface IProjectService
{
    Task<PaginatedResponse<ProjectResponse>> GetAllAsync(string? status, string? query, int page = 1, int pageSize = 10);
    Task<List<ProjectResponse>> GetFeaturedAsync();
    Task<ProjectResponse?> GetByIdAsync(Guid id);
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, Guid userId, string authorName);
    Task<ProjectResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, Guid userId, bool isAdmin);
    Task<bool> DeleteAsync(Guid id, Guid userId, bool isAdmin);
}
