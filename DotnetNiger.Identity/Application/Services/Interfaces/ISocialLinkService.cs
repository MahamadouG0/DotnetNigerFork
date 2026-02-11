// Contrat applicatif Identity: ISocialLinkService
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;

namespace DotnetNiger.Identity.Application.Services.Interfaces;

// Contrat pour la gestion des liens sociaux.
public interface ISocialLinkService
{
	// Liste des liens sociaux de l'utilisateur.
	Task<IReadOnlyList<SocialLinkDto>> GetForUserAsync(Guid userId);

	// Ajout d'un lien social.
	Task<SocialLinkDto> AddAsync(Guid userId, AddSocialLinkRequest request);

	// Suppression d'un lien social.
	Task DeleteAsync(Guid userId, Guid linkId);
}
