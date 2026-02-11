// Entite domaine Identity: RolePermission
namespace DotnetNiger.Identity.Domain.Entities;

// Liaison many-to-many entre roles et permissions.
public class RolePermission
{
	public Guid RoleId { get; set; }
	public Role Role { get; set; } = null!;
	public Guid PermissionId { get; set; }
	public Permission Permission { get; set; } = null!;
}
