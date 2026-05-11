using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Community.Application.DTOs;

public class CreateCommentRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;

    public Guid? PostId { get; set; }
    public Guid? EventId { get; set; }
    public Guid? ParentCommentId { get; set; }
}
