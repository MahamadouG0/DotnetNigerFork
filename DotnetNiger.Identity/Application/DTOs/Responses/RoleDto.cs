// DTO response Identity: RoleDto
namespace DotnetNiger.Identity.Application.DTOs.Responses;

// Reponse pour un role.
public class RoleDto
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
}
