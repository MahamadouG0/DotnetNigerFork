// Entite domaine Identity: Permission
namespace DotnetNiger.Identity.Domain.Entities;

// Permission fonctionnelle associee aux roles.
public class Permission
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
