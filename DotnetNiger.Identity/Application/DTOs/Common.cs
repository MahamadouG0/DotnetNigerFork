namespace DotnetNiger.Identity.Application.DTOs;

public record PaginatedResponse<T>(
    IList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record ErrorResponse(
    string Message,
    string? Code = null,
    IList<string>? Errors = null);
