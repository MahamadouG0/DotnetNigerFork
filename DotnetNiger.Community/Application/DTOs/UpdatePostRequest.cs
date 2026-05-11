using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Community.Application.DTOs;

public class UpdatePostRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Excerpt { get; set; } = string.Empty;

    public string CoverImageUrl { get; set; } = string.Empty;

    [Required]
    public string PostType { get; set; } = string.Empty;

    public List<Guid> CategoryIds { get; set; } = [];
    public List<string> TagNames { get; set; } = [];
    public bool IsPublished { get; set; }
}
