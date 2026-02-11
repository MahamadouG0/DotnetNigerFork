// DTO response Identity: AdminAuditLogDto
namespace DotnetNiger.Identity.Application.DTOs.Responses;

// Reponse admin pour journal des actions.
public class AdminAuditLogDto
{
	public Guid Id { get; set; }
	public Guid AdminUserId { get; set; }
	public string AdminUsername { get; set; } = string.Empty;
	public string AdminEmail { get; set; } = string.Empty;
	public string Action { get; set; } = string.Empty;
	public string TargetType { get; set; } = string.Empty;
	public string TargetId { get; set; } = string.Empty;
	public string Details { get; set; } = string.Empty;
	public string IpAddress { get; set; } = string.Empty;
	public string UserAgent { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
}
