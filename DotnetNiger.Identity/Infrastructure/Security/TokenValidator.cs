// Composant securite Identity: TokenValidator
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DotnetNiger.Identity.Infrastructure.Security;

// Validation manuelle des tokens JWT.
public class TokenValidator
{
	public ClaimsPrincipal Validate(string token, JwtOptions options)
	{
		var handler = new JwtSecurityTokenHandler();
		var parameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = options.Issuer,
			ValidAudience = options.Audience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key))
		};

		return handler.ValidateToken(token, parameters, out _);
	}
}
