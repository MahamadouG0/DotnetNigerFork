// DTO request Identity: RefreshTokenRequest
using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs.Requests;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
