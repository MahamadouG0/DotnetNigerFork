using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Community.Application.DTOs;

public class UpdateCommentRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;
}
