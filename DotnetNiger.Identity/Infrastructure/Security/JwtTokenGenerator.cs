// Composant securite Identity: JwtTokenGenerator
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotnetNiger.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DotnetNiger.Identity.Infrastructure.Security;

public class JwtTokenGenerator
{
	// Generation du token d'acces JWT.
	private readonly JwtOptions _options;
	private readonly UserManager<ApplicationUser> _userManager;

	public JwtTokenGenerator(IOptions<JwtOptions> options, UserManager<ApplicationUser> userManager)
	{
		_options = options.Value;
		_userManager = userManager;
	}

	public async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
	{
		var roles = await _userManager.GetRolesAsync(user);
		var claims = new List<Claim>
		{
			new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
			new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
		};

		foreach (var role in roles)
		{
			claims.Add(new Claim(ClaimTypes.Role, role));
		}

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _options.Issuer,
			audience: _options.Audience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
			signingCredentials: creds);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
