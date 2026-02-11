// Acces donnees Identity: DotnetNigerIdentityDbContext
using DotnetNiger.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Infrastructure.Data;

// Contexte EF Core pour Identity et entites metier.
public class DotnetNigerIdentityDbContext
    : IdentityDbContext<ApplicationUser, Role, Guid>
{
    // Contexte Identity et entites metier.
    public DotnetNigerIdentityDbContext(DbContextOptions<DotnetNigerIdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<SocialLink> SocialLinks => Set<SocialLink>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<AdminActionLog> AdminActionLogs => Set<AdminActionLog>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Liaisons utilisateur -> entites metier.
        builder.Entity<RefreshToken>()
            .HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId);

        builder.Entity<LoginHistory>()
            .HasOne(history => history.User)
            .WithMany(user => user.LoginHistories)
            .HasForeignKey(history => history.UserId);

        builder.Entity<SocialLink>()
            .HasOne(link => link.User)
            .WithMany(user => user.SocialLinks)
            .HasForeignKey(link => link.UserId);

        builder.Entity<ApiKey>()
            .HasOne(key => key.User)
            .WithMany()
            .HasForeignKey(key => key.UserId);

        builder.Entity<AdminActionLog>()
            .HasOne(log => log.AdminUser)
            .WithMany()
            .HasForeignKey(log => log.AdminUserId);

        builder.Entity<Permission>()
            .HasIndex(permission => permission.Name)
            .IsUnique();

        builder.Entity<RolePermission>()
            .HasKey(link => new { link.RoleId, link.PermissionId });

        builder.Entity<RolePermission>()
            .HasOne(link => link.Role)
            .WithMany(role => role.RolePermissions)
            .HasForeignKey(link => link.RoleId);

        builder.Entity<RolePermission>()
            .HasOne(link => link.Permission)
            .WithMany(permission => permission.RolePermissions)
            .HasForeignKey(link => link.PermissionId);
    }
}
