// DTO response Identity: ApiKeyAuditDto
namespace DotnetNiger.Identity.Application.DTOs.Responses;

// Reponse admin pour audit des cles API.
public class ApiKeyAuditDto
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? LastUsedAt { get; set; }
	public DateTime? ExpiresAt { get; set; }
	public bool IsExpired { get; set; }
	public Guid UserId { get; set; }
	public string Username { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
}
