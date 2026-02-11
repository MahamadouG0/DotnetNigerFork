// Contrat applicatif Identity: ITokenService
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;

namespace DotnetNiger.Identity.Application.Services.Interfaces;

public interface ITokenService
{
	// Rafraichissement du token.
	Task<AuthDto> RefreshAsync(RefreshTokenRequest request);
	// Revocation d'un refresh token.
	Task LogoutAsync(Guid userId, RefreshTokenRequest request);
}
