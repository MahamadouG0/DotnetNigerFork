namespace DotnetNiger.Identity.Domain.Entities;

public class TenantClient
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ApplicationId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecretHash { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RedirectUris { get; set; } = "[]";
    public string PostLogoutRedirectUris { get; set; } = "[]";
    public string AllowedGrantTypes { get; set; } = "[\"authorization_code\",\"password\",\"refresh_token\",\"client_credentials\"]";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
