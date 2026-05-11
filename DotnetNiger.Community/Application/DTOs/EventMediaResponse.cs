namespace DotnetNiger.Community.Application.DTOs;

public class EventMediaResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}
