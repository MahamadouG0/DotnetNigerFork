// DTO response Identity: UserSummaryDto
namespace DotnetNiger.Identity.Application.DTOs.Responses;

// Reponse courte pour listing d'utilisateurs.
public class UserSummaryDto
{
	public Guid Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public bool EmailConfirmed { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? LastLoginAt { get; set; }
}
