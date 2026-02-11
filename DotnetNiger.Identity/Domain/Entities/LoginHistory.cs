// Entite domaine Identity: LoginHistory
namespace DotnetNiger.Identity.Domain.Entities;

public class LoginHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime LoginAt { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    // FK
    public ApplicationUser User { get; set; } = null!;
}
