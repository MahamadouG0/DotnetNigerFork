namespace DotnetNiger.Identity.Domain.Entities;

public class UserConsent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ConsentType { get; set; } = string.Empty;
    public string ConsentVersion { get; set; } = string.Empty;
    public bool Granted { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
