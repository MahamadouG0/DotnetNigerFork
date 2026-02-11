// DTO response Identity: ApiKeyDto
namespace DotnetNiger.Identity.Application.DTOs.Responses;

// Reponse de base pour une cle API.
public class ApiKeyDto
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? LastUsedAt { get; set; }
	public DateTime? ExpiresAt { get; set; }
}
