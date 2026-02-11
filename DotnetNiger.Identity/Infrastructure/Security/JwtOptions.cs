// Composant securite Identity: JwtOptions
namespace DotnetNiger.Identity.Infrastructure.Security;

public class JwtOptions
{
	// Parametres JWT issus de la configuration.
	public string Issuer { get; set; } = string.Empty;
	public string Audience { get; set; } = string.Empty;
	public string Key { get; set; } = string.Empty;
	public int AccessTokenMinutes { get; set; } = 60;
	public int RefreshTokenDays { get; set; } = 7;
}
