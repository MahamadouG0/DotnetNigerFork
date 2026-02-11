// Entite domaine Identity: AdminActionLog
namespace DotnetNiger.Identity.Domain.Entities;

// Journal des actions admin.
public class AdminActionLog
{
	public Guid Id { get; set; }
	public Guid AdminUserId { get; set; }
	public string Action { get; set; } = string.Empty;
	public string TargetType { get; set; } = string.Empty;
	public string TargetId { get; set; } = string.Empty;
	public string Details { get; set; } = string.Empty;
	public string IpAddress { get; set; } = string.Empty;
	public string UserAgent { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public ApplicationUser AdminUser { get; set; } = null!;
}
