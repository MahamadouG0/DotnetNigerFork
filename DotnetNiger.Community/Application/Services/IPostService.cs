using DotnetNiger.Community.Application.DTOs;

namespace DotnetNiger.Community.Application.Services;

public interface IPostService
{
    Task<PaginatedResponse<PostResponse>> GetAllAsync(string? published, string? category, string? tag, string? query, int page = 1, int pageSize = 10);
    Task<PostResponse?> GetByIdAsync(Guid id);
    Task<PostResponse> CreateAsync(CreatePostRequest request, Guid authorId, string authorName);
    Task<PostResponse?> UpdateAsync(Guid id, UpdatePostRequest request, Guid userId, bool isAdmin);
    Task<bool> DeleteAsync(Guid id, Guid userId, bool isAdmin);
}
