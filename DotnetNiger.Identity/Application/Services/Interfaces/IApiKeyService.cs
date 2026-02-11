// Contrat applicatif Identity: IApiKeyService
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;

namespace DotnetNiger.Identity.Application.Services.Interfaces;

// Contrat pour la gestion des cles API.
public interface IApiKeyService
{
	Task<ApiKeySecretDto> CreateAsync(Guid userId, CreateApiKeyRequest request);
	Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid userId);
	Task<ApiKeySecretDto> RotateAsync(Guid userId, Guid apiKeyId);
	Task RevokeAsync(Guid userId, Guid apiKeyId);
	Task RevokeAllAsync(Guid userId);
}
