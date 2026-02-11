// Composant securite Identity: ApiKeyHasher
using System.Security.Cryptography;
using System.Text;

namespace DotnetNiger.Identity.Infrastructure.Security;

// Utilitaire de hash pour les cles API.
public static class ApiKeyHasher
{
	public static string Hash(string key)
	{
		var bytes = Encoding.UTF8.GetBytes(key);
		var hash = SHA256.HashData(bytes);
		return Convert.ToBase64String(hash);
	}
}
