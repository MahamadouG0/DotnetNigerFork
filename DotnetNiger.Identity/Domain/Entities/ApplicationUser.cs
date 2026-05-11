using Microsoft.AspNetCore.Identity;

namespace DotnetNiger.Identity.Domain.Entities;

/// <summary>
/// Utilisateur de l'application étendant IdentityUser avec les champs multi-tenant.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? EmailConfirmationCode { get; set; }
    public DateTime? EmailConfirmationCodeExpiry { get; set; }
}
