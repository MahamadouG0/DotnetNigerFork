namespace DotnetNiger.Identity.Application.DTOs;

public record PaginatedResponse<T>(
    IList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 1));
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public record PaginationQuery(int Page = 1, int PageSize = 20)
{
    public int EnsurePage => Math.Max(1, Page);
    public int EnsurePageSize => Math.Clamp(PageSize, 1, 100);
}

public record ErrorResponse(
    string Message,
    string? Code = null,
    IList<string>? Errors = null);
