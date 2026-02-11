// DTO request Identity: AssignPermissionRequest
using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs.Requests;

// Requete pour assigner ou retirer une permission a un role.
public class AssignPermissionRequest
{
	[Required]
	public Guid RoleId { get; set; }

	[Required]
	public Guid PermissionId { get; set; }
}
