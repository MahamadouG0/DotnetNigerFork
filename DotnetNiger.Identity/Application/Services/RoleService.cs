// Service applicatif Identity: RoleService
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Exceptions;
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Application.Services;

// Gestion des roles via ASP.NET Core Identity.
public class RoleService : IRoleService
{
	// Gestion des roles via ASP.NET Core Identity.
	private readonly RoleManager<Role> _roleManager;
	private readonly UserManager<ApplicationUser> _userManager;

	public RoleService(RoleManager<Role> roleManager, UserManager<ApplicationUser> userManager)
	{
		_roleManager = roleManager;
		_userManager = userManager;
	}

	public async Task<IReadOnlyList<RoleDto>> GetAllAsync()
	{
		return await _roleManager.Roles
			.OrderBy(role => role.Name)
			.Select(role => new RoleDto
			{
				Id = role.Id,
				Name = role.Name ?? string.Empty
			})
			.ToListAsync();
	}

	public async Task<RoleDto> CreateAsync(AddRoleRequest request)
	{
		var name = request.Name?.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new IdentityException("Role name is required.", 400);
		}

		var existing = await _roleManager.FindByNameAsync(name);
		if (existing != null)
		{
			throw new IdentityException("Role already exists.", 409);
		}

		var role = new Role(name);
		var result = await _roleManager.CreateAsync(role);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}

		return new RoleDto
		{
			Id = role.Id,
			Name = role.Name ?? string.Empty
		};
	}

	public async Task DeleteAsync(Guid roleId)
	{
		var role = await _roleManager.FindByIdAsync(roleId.ToString());
		if (role == null)
		{
			throw new IdentityException("Role not found.", 404);
		}

		var result = await _roleManager.DeleteAsync(role);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}
	}

	public async Task AssignToUserAsync(AssignRoleRequest request)
	{
		var roleName = request.RoleName?.Trim();
		if (string.IsNullOrWhiteSpace(roleName))
		{
			throw new IdentityException("Role name is required.", 400);
		}

		var user = await _userManager.FindByIdAsync(request.UserId.ToString());
		if (user == null)
		{
			throw new IdentityException("User not found.", 404);
		}

		var role = await _roleManager.FindByNameAsync(roleName);
		if (role == null)
		{
			throw new IdentityException("Role not found.", 404);
		}

		var result = await _userManager.AddToRoleAsync(user, role.Name ?? roleName);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}
	}

	public async Task RemoveFromUserAsync(AssignRoleRequest request)
	{
		var roleName = request.RoleName?.Trim();
		if (string.IsNullOrWhiteSpace(roleName))
		{
			throw new IdentityException("Role name is required.", 400);
		}

		var user = await _userManager.FindByIdAsync(request.UserId.ToString());
		if (user == null)
		{
			throw new IdentityException("User not found.", 404);
		}

		var result = await _userManager.RemoveFromRoleAsync(user, roleName);
		if (!result.Succeeded)
		{
			var message = string.Join(" ", result.Errors.Select(error => error.Description));
			throw new IdentityException(message, 400);
		}
	}

	public async Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId)
	{
		var user = await _userManager.FindByIdAsync(userId.ToString());
		if (user == null)
		{
			throw new IdentityException("User not found.", 404);
		}

		var roles = await _userManager.GetRolesAsync(user);
		return roles.ToList();
	}
}
