// DTO response Identity: TokenDto
namespace DotnetNiger.Identity.Application.DTOs.Responses;

public class TokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } // en secondes
    public string TokenType { get; set; } = "Bearer";
}
