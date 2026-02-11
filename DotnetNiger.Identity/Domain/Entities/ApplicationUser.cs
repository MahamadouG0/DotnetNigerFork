// Entite domaine Identity: ApplicationUser
using Microsoft.AspNetCore.Identity;

namespace DotnetNiger.Identity.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }

    // Relations
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();
    public ICollection<SocialLink> SocialLinks { get; set; } = new List<SocialLink>();
}
