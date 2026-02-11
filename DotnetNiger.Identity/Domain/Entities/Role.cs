// Entite domaine Identity: Role
using Microsoft.AspNetCore.Identity;

namespace DotnetNiger.Identity.Domain.Entities;

// Role applicatif avec relations de permissions.
public class Role : IdentityRole<Guid>
{
	public Role()
	{
	}

	public Role(string name)
		: base(name)
	{
	}

	public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
