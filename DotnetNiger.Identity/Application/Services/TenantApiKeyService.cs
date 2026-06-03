using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.DTOs;

namespace DotnetNiger.Identity.Application.Services;

public class TenantApiKeyService
{
    private readonly IdentityDbContext _db;

    public TenantApiKeyService(IdentityDbContext db) => _db = db;

    public async Task<TenantApiKeyCreatedResponse> CreateApiKeyAsync(Guid tenantId, CreateTenantApiKeyRequest request)
    {
        var secret = GenerateSecret();
        var prefix = "dni_" + secret[..Math.Min(8, secret.Length)].ToLowerInvariant();

        var publicKey = $"pk_{Guid.NewGuid():N}";
        var privateKeyHash = HashSecret(secret);

        var apiKey = new TenantApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            KeyPrefix = prefix,
            PublicKey = publicKey,
            PrivateKeyHash = privateKeyHash,
            Scopes = string.IsNullOrWhiteSpace(request.Scopes)
                ? "[\"api\"]"
                : request.Scopes,
            IsActive = true,
            ExpiresAt = request.ExpiresAt,
        };

        _db.TenantApiKeys.Add(apiKey);
        await _db.SaveChangesAsync();

        return new TenantApiKeyCreatedResponse(
            MapToResponse(apiKey),
            secret);
    }

    public async Task<PaginatedResponse<TenantApiKeyResponse>> GetApiKeysAsync(Guid tenantId, PaginationQuery pagination)
    {
        var query = _db.TenantApiKeys.Where(k => k.TenantId == tenantId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(k => k.CreatedAt)
            .Skip((pagination.EnsurePage - 1) * pagination.EnsurePageSize)
            .Take(pagination.EnsurePageSize)
            .ToListAsync();

        return new PaginatedResponse<TenantApiKeyResponse>(
            items.Select(MapToResponse).ToList(), totalCount, pagination.EnsurePage, pagination.EnsurePageSize);
    }

    public async Task<TenantApiKeyResponse> GetApiKeyByIdAsync(Guid tenantId, Guid keyId)
    {
        var key = await _db.TenantApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId && k.TenantId == tenantId)
            ?? throw new KeyNotFoundException("Clé API non trouvée");

        return MapToResponse(key);
    }

    public async Task DeleteApiKeyAsync(Guid tenantId, Guid keyId)
    {
        var key = await _db.TenantApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId && k.TenantId == tenantId)
            ?? throw new KeyNotFoundException("Clé API non trouvée");

        _db.TenantApiKeys.Remove(key);
        await _db.SaveChangesAsync();
    }

    public async Task<TenantApiKeyCreatedResponse> RotateApiKeyAsync(Guid tenantId, Guid keyId)
    {
        var key = await _db.TenantApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId && k.TenantId == tenantId)
            ?? throw new KeyNotFoundException("Clé API non trouvée");

        var secret = GenerateSecret();
        var prefix = "dni_" + secret[..Math.Min(8, secret.Length)].ToLowerInvariant();
        var privateKeyHash = HashSecret(secret);

        key.KeyPrefix = prefix;
        key.PrivateKeyHash = privateKeyHash;
        key.LastUsedAt = null;

        await _db.SaveChangesAsync();

        return new TenantApiKeyCreatedResponse(
            MapToResponse(key),
            secret);
    }

    private static TenantApiKeyResponse MapToResponse(TenantApiKey k)
    {
        return new TenantApiKeyResponse(
            k.Id, k.TenantId, k.Name, k.KeyPrefix, k.PublicKey,
            JsonSerializer.Deserialize<List<string>>(k.Scopes) ?? ["api"],
            k.IsActive, k.CreatedAt, k.ExpiresAt, k.LastUsedAt);
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(bytes);
    }
}
