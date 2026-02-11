// Entite domaine Identity: ApiKey
namespace DotnetNiger.Identity.Domain.Entities;

public class ApiKey
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Key { get; set; } = string.Empty; // Hasé en BD
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // FK
    public ApplicationUser User { get; set; } = null!;
}
