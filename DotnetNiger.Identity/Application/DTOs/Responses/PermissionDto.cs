// DTO response Identity: PermissionDto
namespace DotnetNiger.Identity.Application.DTOs.Responses;

// Reponse pour une permission.
public class PermissionDto
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
}
