namespace DotnetNiger.Identity.Domain.Entities;

public class ExternalService
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ApiKeyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string HealthEndpoint { get; set; } = "/health";
    public bool IsActive { get; set; } = true;
    public ExternalServiceStatus Status { get; set; } = ExternalServiceStatus.Pending;
    public DateTime? LastHealthCheckAt { get; set; }
    public int HealthCheckFailures { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
