// DTO request Identity: CreateApiKeyRequest
using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs.Requests;

// Requete de creation d'une cle API.
public class CreateApiKeyRequest
{
	[Required]
	public string Name { get; set; } = string.Empty;

	public DateTime? ExpiresAt { get; set; }
}
