using DotnetNiger.Community.Infrastructure;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Application.Services;

public class ProfileService(AppDbContext db) : IProfileService
{
    public async Task<ProfileResponse?> GetAsync(Guid userId)
    {
        var member = await db.Members
            .Include(m => m.SocialLinks)
            .FirstOrDefaultAsync(m => m.Id == userId);
        return member is null ? null : MapProfile(member);
    }

    public async Task<ProfileResponse> UpdateAsync(Guid userId, UpdateProfileRequest request)
    {
        var member = await db.Members
            .Include(m => m.SocialLinks)
            .FirstOrDefaultAsync(m => m.Id == userId);

        if (member is null)
        {
            member = new Member
            {
                Id = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Members.Add(member);
        }

        if (request.FullName is not null) member.FullName = request.FullName;
        if (request.PhoneNumber is not null) member.PhoneNumber = request.PhoneNumber;
        if (request.Bio is not null) member.Bio = request.Bio;
        if (request.AvatarUrl is not null) member.AvatarUrl = request.AvatarUrl;
        if (request.Country is not null) member.Country = request.Country;
        if (request.City is not null) member.City = request.City;
        member.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return MapProfile(member);
    }

    public async Task<SocialLinkResponse> AddSocialLinkAsync(Guid userId, AddSocialLinkRequest request)
    {
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == userId);
        if (member is null)
        {
            member = new Member { Id = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.Members.Add(member);
        }

        var link = new SocialLink
        {
            Id = Guid.NewGuid(),
            MemberId = userId,
            Platform = request.Platform,
            Url = request.Url
        };

        db.SocialLinks.Add(link);
        await db.SaveChangesAsync();
        return new SocialLinkResponse { Id = link.Id, Platform = link.Platform, Url = link.Url };
    }

    public async Task<bool> DeleteSocialLinkAsync(Guid userId, Guid socialLinkId)
    {
        var link = await db.SocialLinks.FirstOrDefaultAsync(s => s.Id == socialLinkId && s.MemberId == userId);
        if (link is null) return false;
        db.SocialLinks.Remove(link);
        await db.SaveChangesAsync();
        return true;
    }

    private static ProfileResponse MapProfile(Member m) => new()
    {
        Id = m.Id,
        FullName = m.FullName,
        Bio = m.Bio,
        AvatarUrl = m.AvatarUrl,
        PhoneNumber = m.PhoneNumber,
        Country = m.Country,
        City = m.City,
        CreatedAt = m.CreatedAt,
        SocialLinks = m.SocialLinks.Select(s => new SocialLinkResponse
        {
            Id = s.Id,
            Platform = s.Platform,
            Url = s.Url
        }).ToList()
    };
}
