// DTO response Identity: ApiKeySecretDto
namespace DotnetNiger.Identity.Application.DTOs.Responses;

// Reponse avec la cle en clair (uniquement a la creation/rotation).
public class ApiKeySecretDto
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Key { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? ExpiresAt { get; set; }
}
