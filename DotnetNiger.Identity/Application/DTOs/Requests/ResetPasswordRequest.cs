// DTO request Identity: ResetPasswordRequest
using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs.Requests;

// Requete de reinitialisation de mot de passe.
public class ResetPasswordRequest
{
	[Required]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string Token { get; set; } = string.Empty;

	[Required]
	public string NewPassword { get; set; } = string.Empty;
}
