// Exception metier Identity: InvalidCredentialsException
namespace DotnetNiger.Identity.Application.Exceptions;

public class InvalidCredentialsException : IdentityException
{
	// Identifiants invalides.
	public InvalidCredentialsException()
		: base("Invalid credentials.", 401)
	{
	}
}
