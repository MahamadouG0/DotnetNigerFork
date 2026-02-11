// Composant securite Identity: RefreshTokenGenerator
using System.Security.Cryptography;

namespace DotnetNiger.Identity.Infrastructure.Security;

public class RefreshTokenGenerator
{
	// Generation d'un refresh token aleatoire.
	public string GenerateToken()
	{
		var bytes = RandomNumberGenerator.GetBytes(64);
		return Convert.ToBase64String(bytes);
	}
}
