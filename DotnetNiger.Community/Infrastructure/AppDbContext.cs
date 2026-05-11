using DotnetNiger.Community.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PostCategory> PostCategories => Set<PostCategory>();
    public DbSet<PostTag> PostTags => Set<PostTag>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventMedia> EventMedias => Set<EventMedia>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ResourceCategory> ResourceCategories => Set<ResourceCategory>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<SocialLink> SocialLinks => Set<SocialLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Slug).HasMaxLength(200);
            entity.Property(e => e.PostType).HasMaxLength(50);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Slug).HasMaxLength(100);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Slug).HasMaxLength(100);
        });

        modelBuilder.Entity<PostCategory>(entity =>
        {
            entity.HasKey(e => new { e.PostId, e.CategoryId });
            entity.HasOne(e => e.Post).WithMany(e => e.PostCategories).HasForeignKey(e => e.PostId);
            entity.HasOne(e => e.Category).WithMany(e => e.PostCategories).HasForeignKey(e => e.CategoryId);
        });

        modelBuilder.Entity<PostTag>(entity =>
        {
            entity.HasKey(e => new { e.PostId, e.TagId });
            entity.HasOne(e => e.Post).WithMany(e => e.PostTags).HasForeignKey(e => e.PostId);
            entity.HasOne(e => e.Tag).WithMany(e => e.PostTags).HasForeignKey(e => e.TagId);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Slug).HasMaxLength(200);
            entity.Property(e => e.EventType).HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(200);
        });

        modelBuilder.Entity<EventMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Event).WithMany(e => e.Medias).HasForeignKey(e => e.EventId);
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        modelBuilder.Entity<EventRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Event).WithMany(e => e.Registrations).HasForeignKey(e => e.EventId);
            entity.HasIndex(e => new { e.EventId, e.UserId }).IsUnique();
            entity.Property(e => e.RegistrationStatus).HasMaxLength(50);
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Slug).HasMaxLength(200);
            entity.Property(e => e.ResourceType).HasMaxLength(50);
            entity.Property(e => e.Level).HasMaxLength(50);
        });

        modelBuilder.Entity<ResourceCategory>(entity =>
        {
            entity.HasKey(e => new { e.ResourceId, e.CategoryId });
            entity.HasOne(e => e.Resource).WithMany(e => e.ResourceCategories).HasForeignKey(e => e.ResourceId);
            entity.HasOne(e => e.Category).WithMany(e => e.ResourceCategories).HasForeignKey(e => e.CategoryId);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Post).WithMany(e => e.Comments).HasForeignKey(e => e.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Event).WithMany(e => e.Comments).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ParentComment).WithMany(e => e.Replies).HasForeignKey(e => e.ParentCommentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
        });

        modelBuilder.Entity<SocialLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Member).WithMany(e => e.SocialLinks).HasForeignKey(e => e.MemberId);
            entity.Property(e => e.Platform).HasMaxLength(50);
        });
    }
}
