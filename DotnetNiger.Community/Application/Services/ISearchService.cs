using DotnetNiger.Community.Application.DTOs;

namespace DotnetNiger.Community.Application.Services;

public interface ISearchService
{
    Task<PaginatedResponse<SearchResultResponse>> SearchAsync(SearchQueryRequest request);
}
