using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Community.Application.DTOs;

public class CreateResourceRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, Url]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string ResourceType { get; set; } = string.Empty;

    [Required]
    public string Level { get; set; } = string.Empty;

    public List<Guid> CategoryIds { get; set; } = [];
}
