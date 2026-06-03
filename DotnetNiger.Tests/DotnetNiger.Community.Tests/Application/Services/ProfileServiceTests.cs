using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class ProfileServiceTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var svc = new ProfileService(db);
        var result = await svc.GetAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ReturnsProfile_WithSocialLinks()
    {
        var db = CreateDb();
        var memberId = Guid.NewGuid();
        db.Members.Add(new Member
        {
            Id = memberId,
            FullName = "Test User",
            Bio = "Bio text"
        });
        db.SocialLinks.Add(new SocialLink
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            Platform = "GitHub",
            Url = "https://github.com/test"
        });
        await db.SaveChangesAsync();

        var svc = new ProfileService(db);
        var result = await svc.GetAsync(memberId);

        result.Should().NotBeNull();
        result!.FullName.Should().Be("Test User");
        result.SocialLinks.Should().HaveCount(1);
        result.SocialLinks[0].Platform.Should().Be("GitHub");
    }

    [Fact]
    public async Task UpdateAsync_CreatesMemberIfNotExists()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var svc = new ProfileService(db);

        var result = await svc.UpdateAsync(userId, new UpdateProfileRequest
        {
            FullName = "New Member",
            Bio = "Just joined"
        });

        result.FullName.Should().Be("New Member");
        db.Members.Count().Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingMember()
    {
        var db = CreateDb();
        var memberId = Guid.NewGuid();
        db.Members.Add(new Member { Id = memberId, FullName = "Original" });
        await db.SaveChangesAsync();

        var svc = new ProfileService(db);
        var result = await svc.UpdateAsync(memberId, new UpdateProfileRequest
        {
            FullName = "Updated",
            Bio = "Updated bio"
        });

        result.FullName.Should().Be("Updated");
        result.Bio.Should().Be("Updated bio");
    }

    [Fact]
    public async Task AddSocialLinkAsync_AddsLinkToMember()
    {
        var db = CreateDb();
        var memberId = Guid.NewGuid();
        db.Members.Add(new Member { Id = memberId });
        await db.SaveChangesAsync();

        var svc = new ProfileService(db);
        var result = await svc.AddSocialLinkAsync(memberId, new AddSocialLinkRequest
        {
            Platform = "Twitter",
            Url = "https://twitter.com/test"
        });

        result.Platform.Should().Be("Twitter");
        db.SocialLinks.Count().Should().Be(1);
    }

    [Fact]
    public async Task DeleteSocialLinkAsync_RemovesLink()
    {
        var db = CreateDb();
        var memberId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        db.Members.Add(new Member { Id = memberId });
        db.SocialLinks.Add(new SocialLink { Id = linkId, MemberId = memberId, Platform = "X", Url = "https://x.com" });
        await db.SaveChangesAsync();

        var svc = new ProfileService(db);
        var result = await svc.DeleteSocialLinkAsync(memberId, linkId);

        result.Should().BeTrue();
        db.SocialLinks.Count().Should().Be(0);
    }

    [Fact]
    public async Task DeleteSocialLinkAsync_WrongMember_ReturnsFalse()
    {
        var db = CreateDb();
        var linkId = Guid.NewGuid();
        db.SocialLinks.Add(new SocialLink { Id = linkId, MemberId = Guid.NewGuid(), Platform = "X", Url = "https://x.com" });
        await db.SaveChangesAsync();

        var svc = new ProfileService(db);
        var result = await svc.DeleteSocialLinkAsync(Guid.NewGuid(), linkId);

        result.Should().BeFalse();
    }
}
