// DTO request Identity: AssignRoleRequest
using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs.Requests;

// Requete pour assigner ou retirer un role.
public class AssignRoleRequest
{
	[Required]
	public Guid UserId { get; set; }

	[Required]
	public string RoleName { get; set; } = string.Empty;
}
