// DTO request Identity: VerifyEmailRequest
using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs.Requests;

// Requete de verification d'email.
public class VerifyEmailRequest
{
	[Required]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string Token { get; set; } = string.Empty;
}
