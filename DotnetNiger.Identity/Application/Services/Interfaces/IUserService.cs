// Contrat applicatif Identity: IUserService
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;

namespace DotnetNiger.Identity.Application.Services.Interfaces;

// Contrat pour les operations utilisateur.
public interface IUserService
{
	// Profil utilisateur courant.
	Task<UserDto> GetProfileAsync(Guid userId);

	// Mise a jour du profil utilisateur.
	Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);

	// Mise a jour de l'avatar utilisateur.
	Task<UserDto> UpdateAvatarAsync(Guid userId, string avatarUrl);

	// Suppression de l'avatar utilisateur.
	Task<UserDto> ClearAvatarAsync(Guid userId);

	// Changement de mot de passe.
	Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

	// Changement d'email.
	Task ChangeEmailAsync(Guid userId, ChangeEmailRequest request);
}
