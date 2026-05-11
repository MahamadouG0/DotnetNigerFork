using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Community.Application.DTOs;

public class CreateRoleRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
