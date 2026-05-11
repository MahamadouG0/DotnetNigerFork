namespace DotnetNiger.Community.Application.DTOs;

public class ApiSuccessResponse<T>
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public T? Data { get; set; }
}
