// Service applicatif Identity: PermissionService
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Exceptions;
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Application.Services;

// Gestion des permissions et associations role/permission.
public class PermissionService : IPermissionService
{
	private readonly DotnetNigerIdentityDbContext _dbContext;
	private readonly RoleManager<Role> _roleManager;

	public PermissionService(DotnetNigerIdentityDbContext dbContext, RoleManager<Role> roleManager)
	{
		_dbContext = dbContext;
		_roleManager = roleManager;
	}

	public async Task<IReadOnlyList<PermissionDto>> GetAllAsync()
	{
		return await _dbContext.Permissions
			.OrderBy(permission => permission.Name)
			.Select(permission => new PermissionDto
			{
				Id = permission.Id,
				Name = permission.Name,
				Description = permission.Description
			})
			.ToListAsync();
	}

	public async Task<PermissionDto> CreateAsync(AddPermissionRequest request)
	{
		var name = request.Name?.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new IdentityException("Permission name is required.", 400);
		}

		var exists = await _dbContext.Permissions.AnyAsync(permission => permission.Name == name);
		if (exists)
		{
			throw new IdentityException("Permission already exists.", 409);
		}

		var permission = new Permission
		{
			Name = name,
			Description = request.Description?.Trim() ?? string.Empty
		};

		_dbContext.Permissions.Add(permission);
		await _dbContext.SaveChangesAsync();

		return new PermissionDto
		{
			Id = permission.Id,
			Name = permission.Name,
			Description = permission.Description
		};
	}

	public async Task DeleteAsync(Guid permissionId)
	{
		var permission = await _dbContext.Permissions.FirstOrDefaultAsync(item => item.Id == permissionId);
		if (permission == null)
		{
			throw new IdentityException("Permission not found.", 404);
		}

		var links = await _dbContext.RolePermissions
			.Where(link => link.PermissionId == permission.Id)
			.ToListAsync();
		if (links.Count > 0)
		{
			_dbContext.RolePermissions.RemoveRange(links);
		}

		_dbContext.Permissions.Remove(permission);
		await _dbContext.SaveChangesAsync();
	}

	public async Task AssignToRoleAsync(AssignPermissionRequest request)
	{
		var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
		if (role == null)
		{
			throw new IdentityException("Role not found.", 404);
		}

		var permission = await _dbContext.Permissions.FirstOrDefaultAsync(item => item.Id == request.PermissionId);
		if (permission == null)
		{
			throw new IdentityException("Permission not found.", 404);
		}

		var exists = await _dbContext.RolePermissions.AnyAsync(link =>
			link.RoleId == role.Id && link.PermissionId == permission.Id);
		if (exists)
		{
			throw new IdentityException("Permission already assigned.", 409);
		}

		_dbContext.RolePermissions.Add(new RolePermission
		{
			RoleId = role.Id,
			PermissionId = permission.Id
		});

		await _dbContext.SaveChangesAsync();
	}

	public async Task RemoveFromRoleAsync(AssignPermissionRequest request)
	{
		var link = await _dbContext.RolePermissions
			.FirstOrDefaultAsync(item => item.RoleId == request.RoleId && item.PermissionId == request.PermissionId);
		if (link == null)
		{
			throw new IdentityException("Role permission not found.", 404);
		}

		_dbContext.RolePermissions.Remove(link);
		await _dbContext.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(Guid roleId)
	{
		var role = await _roleManager.FindByIdAsync(roleId.ToString());
		if (role == null)
		{
			throw new IdentityException("Role not found.", 404);
		}

		return await _dbContext.RolePermissions
			.Where(link => link.RoleId == role.Id)
			.Select(link => new PermissionDto
			{
				Id = link.Permission.Id,
				Name = link.Permission.Name,
				Description = link.Permission.Description
			})
			.OrderBy(permission => permission.Name)
			.ToListAsync();
	}
}
