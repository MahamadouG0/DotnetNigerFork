// Entite domaine Identity: SocialLink
namespace DotnetNiger.Identity.Domain.Entities;

public class SocialLink
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Platform { get; set; } = string.Empty; // Twitter, LinkedIn, GitHub, Facebook
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // FK
    public ApplicationUser User { get; set; } = null!;
}
