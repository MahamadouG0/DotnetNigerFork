// Service applicatif Identity: PasswordService
using System.Security.Cryptography;
using DotnetNiger.Identity.Application.Services.Interfaces;

namespace DotnetNiger.Identity.Application.Services;

// Service d'utilitaires pour mots de passe.
public class PasswordService : IPasswordService
{
	private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";

	public string GenerateRandom(int length = 16)
	{
		if (length <= 0)
		{
			length = 16;
		}

		var bytes = RandomNumberGenerator.GetBytes(length);
		var chars = new char[length];
		for (var index = 0; index < length; index++)
		{
			chars[index] = Alphabet[bytes[index] % Alphabet.Length];
		}

		return new string(chars);
	}

	public bool IsStrong(string password)
	{
		if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
		{
			return false;
		}

		var hasUpper = password.Any(char.IsUpper);
		var hasLower = password.Any(char.IsLower);
		var hasDigit = password.Any(char.IsDigit);
		var hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

		return hasUpper && hasLower && hasDigit && hasSymbol;
	}
}
