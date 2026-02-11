// Contrat applicatif Identity: IAvatarMetadataService
using DotnetNiger.Identity.Application.DTOs.Responses;

namespace DotnetNiger.Identity.Application.Services.Interfaces;

// Service de lecture des metadonnees d'avatar.
public interface IAvatarMetadataService
{
	Task<AvatarInfoDto> GetMetadataAsync(string? avatarUrl);
}
