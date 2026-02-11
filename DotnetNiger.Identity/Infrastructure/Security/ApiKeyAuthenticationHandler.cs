// Composant securite Identity: ApiKeyAuthenticationHandler
using System.Security.Claims;
using System.Text.Encodings.Web;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetNiger.Identity.Infrastructure.Security;

// Authentification via cle API.
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	private const string HeaderName = "X-API-Key";
	private readonly DotnetNigerIdentityDbContext _dbContext;
	private readonly UserManager<ApplicationUser> _userManager;

	public ApiKeyAuthenticationHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder,
		ISystemClock clock,
		DotnetNigerIdentityDbContext dbContext,
		UserManager<ApplicationUser> userManager)
		: base(options, logger, encoder, clock)
	{
		_dbContext = dbContext;
		_userManager = userManager;
	}

	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.TryGetValue(HeaderName, out var values))
		{
			return AuthenticateResult.NoResult();
		}

		var rawKey = values.ToString();
		if (string.IsNullOrWhiteSpace(rawKey))
		{
			return AuthenticateResult.Fail("Missing API key.");
		}

		var hashedKey = ApiKeyHasher.Hash(rawKey.Trim());
		var apiKey = await _dbContext.ApiKeys
			.Include(key => key.User)
			.FirstOrDefaultAsync(key => key.Key == hashedKey);

		if (apiKey == null)
		{
			return AuthenticateResult.Fail("Invalid API key.");
		}

		if (!apiKey.IsActive)
		{
			return AuthenticateResult.Fail("API key inactive.");
		}

		if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value <= DateTime.UtcNow)
		{
			return AuthenticateResult.Fail("API key expired.");
		}

		var user = apiKey.User;
		if (user == null || !user.IsActive)
		{
			return AuthenticateResult.Fail("User disabled.");
		}

		apiKey.LastUsedAt = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync();

		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
			new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
			new Claim("auth_method", "api_key"),
			new Claim("scope", "api_key"),
			new Claim("api_key_id", apiKey.Id.ToString())
		};

		var roles = await _userManager.GetRolesAsync(user);
		foreach (var role in roles)
		{
			claims.Add(new Claim(ClaimTypes.Role, role));
		}

		var identity = new ClaimsIdentity(claims, Scheme.Name);
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, Scheme.Name);

		return AuthenticateResult.Success(ticket);
	}
}
