namespace DotnetNiger.Community.Application.DTOs;

public class SearchResultResponse
{
    public string Type { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime CreatedAt { get; set; }
}
