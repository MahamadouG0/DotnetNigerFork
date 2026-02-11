// DTO request Identity: LoginRequest
using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs.Requests;

public class LoginRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
