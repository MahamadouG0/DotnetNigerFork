// DTO request Identity: ForgotPasswordRequest
using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs.Requests;

// Requete de demande de reinitialisation.
public class ForgotPasswordRequest
{
	[Required]
	public string Email { get; set; } = string.Empty;
}
