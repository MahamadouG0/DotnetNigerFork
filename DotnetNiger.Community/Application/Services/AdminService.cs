using DotnetNiger.Community.Infrastructure;
using DotnetNiger.Community.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Application.Services;

public class AdminService(AppDbContext db, IIdentityApiClient identity) : IAdminService
{
    public async Task<DashboardResponse> GetDashboardAsync()
    {
        var postsCount = await db.Posts.CountAsync();
        var publishedPosts = await db.Posts.CountAsync(p => p.IsPublished);
        var eventsCount = await db.Events.CountAsync();
        var upcomingEvents = await db.Events.CountAsync(e => e.IsPublished && e.EndDate >= DateTime.UtcNow);
        var resourcesCount = await db.Resources.CountAsync();
        var membersCount = await db.Members.CountAsync();
        var commentsCount = await db.Comments.CountAsync();

        return new DashboardResponse
        {
            PostsCount = postsCount,
            PublishedPostsCount = publishedPosts,
            EventsCount = eventsCount,
            UpcomingEventsCount = upcomingEvents,
            ResourcesCount = resourcesCount,
            MembersCount = membersCount,
            CommentsCount = commentsCount
        };
    }

    public async Task<List<UserDto>> GetUsersAsync() => await identity.GetUsersAsync();

    public async Task<UserDto?> GetUserAsync(Guid id) => await identity.GetUserAsync(id);

    public async Task<bool> UpdateUserStatusAsync(Guid id, bool isActive) => await identity.UpdateUserStatusAsync(id, isActive);

    public async Task<List<RoleDto>> GetRolesAsync() => await identity.GetRolesAsync();

    public async Task<RoleDto?> CreateRoleAsync(string name) => await identity.CreateRoleAsync(name);

    public async Task<List<PermissionDto>> GetPermissionsAsync() => await identity.GetPermissionsAsync();

    public async Task<PermissionDto?> CreatePermissionAsync(string name, string description) => await identity.CreatePermissionAsync(name, description);

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId) => await identity.AssignPermissionToRoleAsync(roleId, permissionId);

    public async Task<bool> AssignRoleToUserAsync(Guid userId, string roleName) => await identity.AssignRoleToUserAsync(userId, roleName);
}
