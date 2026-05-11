using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Community.Application.DTOs;

public class AssignPermissionRequest
{
    [Required]
    public Guid PermissionId { get; set; }
}
