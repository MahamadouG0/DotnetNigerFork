// DTO request Identity: AddRoleRequest
using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs.Requests;

// Requete de creation d'un role.
public class AddRoleRequest
{
	[Required]
	public string Name { get; set; } = string.Empty;
}
