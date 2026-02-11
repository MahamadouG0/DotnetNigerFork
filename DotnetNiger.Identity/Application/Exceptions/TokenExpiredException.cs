// Exception metier Identity: TokenExpiredException
namespace DotnetNiger.Identity.Application.Exceptions;

public class TokenExpiredException : IdentityException
{
	// Token expire.
	public TokenExpiredException()
		: base("Refresh token expired.", 401)
	{
	}
}
