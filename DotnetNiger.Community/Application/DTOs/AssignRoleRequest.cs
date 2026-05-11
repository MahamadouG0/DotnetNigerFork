using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Community.Application.DTOs;

public class AssignRoleRequest
{
    [Required]
    public string RoleName { get; set; } = string.Empty;
}
