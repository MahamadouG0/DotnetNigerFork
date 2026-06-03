using DotnetNiger.Community.Infrastructure;
using DotnetNiger.Community.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Application.Services;

public class MemberDirectoryService(AppDbContext db) : IMemberDirectoryService
{
    public async Task<PaginatedResponse<MemberDirectoryResponse>> GetAllAsync(string? query, string? country, int page = 1, int pageSize = 10)
    {
        var q = db.Members
            .Include(m => m.SocialLinks)
            .Where(m => !string.IsNullOrWhiteSpace(m.FullName))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(m => m.FullName.Contains(query) || m.Bio.Contains(query) || m.City.Contains(query));
        if (!string.IsNullOrWhiteSpace(country))
            q = q.Where(m => m.Country == country);

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(m => m.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => MapMember(m))
            .ToListAsync();

        return new PaginatedResponse<MemberDirectoryResponse> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<MemberDirectoryResponse?> GetByIdAsync(Guid id)
    {
        var m = await db.Members.Include(m => m.SocialLinks).FirstOrDefaultAsync(m => m.Id == id);
        return m is null ? null : MapMember(m);
    }

    private static MemberDirectoryResponse MapMember(Domain.Entities.Member m) => new()
    {
        Id = m.Id,
        FullName = m.FullName,
        Bio = m.Bio,
        AvatarUrl = m.AvatarUrl,
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
