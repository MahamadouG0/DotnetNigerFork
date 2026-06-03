namespace DotnetNiger.Identity.Domain.Entities;

/// <summary>
/// Represents an audit log entry for tracking administrative actions.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The ID of the tenant where the action occurred (null for system-level actions).
    /// </summary>
    public Guid? TenantId { get; set; }
    
    /// <summary>
    /// The ID of the user who performed the action.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// The type of entity that was acted upon (e.g., "Tenant", "User", "Role", "Permission", "Client", "ApiKey", "ExternalService").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the entity that was acted upon.
    /// </summary>
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// The action performed (e.g., "Create", "Update", "Delete", "Activate", "Deactivate", "AssignPermission", "RevokePermission").
    /// </summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// A description of the changes made (can be JSON or formatted text).
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The IP address from which the action originated.
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// The date and time when the action was performed.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}