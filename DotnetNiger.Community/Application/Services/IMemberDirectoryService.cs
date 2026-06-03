using DotnetNiger.Community.Application.DTOs;

namespace DotnetNiger.Community.Application.Services;

public interface IMemberDirectoryService
{
    Task<PaginatedResponse<MemberDirectoryResponse>> GetAllAsync(string? query, string? country, int page = 1, int pageSize = 10);
    Task<MemberDirectoryResponse?> GetByIdAsync(Guid id);
}
