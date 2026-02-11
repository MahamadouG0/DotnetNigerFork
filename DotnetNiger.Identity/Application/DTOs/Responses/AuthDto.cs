// DTO response Identity: AuthDto
namespace DotnetNiger.Identity.Application.DTOs.Responses;

public class AuthDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public TokenDto Token { get; set; } = null!;
}
