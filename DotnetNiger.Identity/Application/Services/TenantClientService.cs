using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.DTOs;

namespace DotnetNiger.Identity.Application.Services;

public class TenantClientService
{
    private readonly IdentityDbContext _db;
    private readonly IOpenIddictApplicationManager _applicationManager;

    public TenantClientService(IdentityDbContext db, IOpenIddictApplicationManager applicationManager)
    {
        _db = db;
        _applicationManager = applicationManager;
    }

    public async Task<TenantClientCreatedResponse> CreateClientAsync(Guid tenantId, CreateTenantClientRequest request)
    {
        var tenant = await _db.Tenants.FindAsync(tenantId)
            ?? throw new KeyNotFoundException("Tenant non trouvé");

        var clientId = $"app_{Guid.NewGuid():N}";
        var clientSecret = GenerateSecret();
        var clientSecretHash = HashSecret(clientSecret);

        var grantTypes = ParseJsonArrayOrDefault(request.AllowedGrantTypes,
            ["authorization_code", "password", "refresh_token", "client_credentials"]);
        var redirectUris = ParseJsonArray(request.RedirectUris);
        var postLogoutRedirectUris = ParseJsonArray(request.PostLogoutRedirectUris);

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = request.ClientName,
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
        };

        descriptor.Permissions.Add("ep:token");
        descriptor.Permissions.Add("ep:authorization");
        descriptor.Permissions.Add("ep:logout");
        descriptor.Permissions.Add("ep:userinfo");

        foreach (var grant in grantTypes)
        {
            descriptor.Permissions.Add(grant switch
            {
                "authorization_code" => "gt:authorization_code",
                "password" => "gt:password",
                "refresh_token" => "gt:refresh_token",
                "client_credentials" => "gt:client_credentials",
                _ => throw new InvalidOperationException($"Grant type non supporté : {grant}")
            });
        }

        descriptor.Permissions.Add("scp:openid");
        descriptor.Permissions.Add("scp:email");
        descriptor.Permissions.Add("scp:profile");
        descriptor.Permissions.Add("scp:roles");
        descriptor.Permissions.Add("scp:offline_access");
        descriptor.Permissions.Add("scp:api");

        foreach (var uri in redirectUris)
            descriptor.RedirectUris.Add(new Uri(uri));
        foreach (var uri in postLogoutRedirectUris)
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri));

        var existing = await _applicationManager.FindByClientIdAsync(clientId);
        if (existing != null)
            throw new InvalidOperationException($"Un client avec l'identifiant {clientId} existe déjà.");

        var app = await _applicationManager.CreateAsync(descriptor);
        if (app == null)
            throw new InvalidOperationException("Échec de la création de l'application OpenIddict.");

        var applicationId = await _applicationManager.GetIdAsync(app);

        var tenantClient = new TenantClient
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ApplicationId = applicationId!,
            ClientId = clientId,
            ClientSecretHash = clientSecretHash,
            ClientName = request.ClientName,
            Description = request.Description,
            RedirectUris = JsonSerializer.Serialize(redirectUris),
            PostLogoutRedirectUris = JsonSerializer.Serialize(postLogoutRedirectUris),
            AllowedGrantTypes = JsonSerializer.Serialize(grantTypes),
            IsActive = true,
        };

        _db.TenantClients.Add(tenantClient);
        await _db.SaveChangesAsync();

        return new TenantClientCreatedResponse(
            MapToResponse(tenantClient),
            clientSecret);
    }

    public async Task<PaginatedResponse<TenantClientResponse>> GetClientsAsync(Guid tenantId, PaginationQuery pagination)
    {
        var query = _db.TenantClients.Where(c => c.TenantId == tenantId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pagination.EnsurePage - 1) * pagination.EnsurePageSize)
            .Take(pagination.EnsurePageSize)
            .ToListAsync();

        return new PaginatedResponse<TenantClientResponse>(
            items.Select(MapToResponse).ToList(), totalCount, pagination.EnsurePage, pagination.EnsurePageSize);
    }

    public async Task<List<TenantClientResponse>> GetClientsByClientIdAsync(string clientId)
    {
        var clients = await _db.TenantClients
            .IgnoreQueryFilters()
            .Where(c => c.ClientId == clientId)
            .ToListAsync();

        return clients.Select(MapToResponse).ToList();
    }

    public async Task<TenantClientResponse> GetClientByIdAsync(Guid tenantId, Guid clientId)
    {
        var client = await _db.TenantClients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId)
            ?? throw new KeyNotFoundException("Client OAuth non trouvé");

        return MapToResponse(client);
    }

    public async Task<TenantClientResponse> UpdateClientAsync(Guid tenantId, Guid clientId, UpdateTenantClientRequest request)
    {
        var tenantClient = await _db.TenantClients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId)
            ?? throw new KeyNotFoundException("Client OAuth non trouvé");

        var app = await _applicationManager.FindByIdAsync(tenantClient.ApplicationId)
            ?? throw new InvalidOperationException("Application OpenIddict introuvable");

        if (request.ClientName != null) tenantClient.ClientName = request.ClientName;
        if (request.Description != null) tenantClient.Description = request.Description;
        if (request.IsActive.HasValue) tenantClient.IsActive = request.IsActive.Value;

        var needOpenIddictUpdate = false;
        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, app);

        if (request.RedirectUris != null)
        {
            tenantClient.RedirectUris = request.RedirectUris;
            var uris = ParseJsonArray(request.RedirectUris);
            descriptor.RedirectUris.Clear();
            foreach (var uri in uris)
                descriptor.RedirectUris.Add(new Uri(uri));
            needOpenIddictUpdate = true;
        }

        if (request.PostLogoutRedirectUris != null)
        {
            tenantClient.PostLogoutRedirectUris = request.PostLogoutRedirectUris;
            var uris = ParseJsonArray(request.PostLogoutRedirectUris);
            descriptor.PostLogoutRedirectUris.Clear();
            foreach (var uri in uris)
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
            needOpenIddictUpdate = true;
        }

        if (request.AllowedGrantTypes != null)
        {
            tenantClient.AllowedGrantTypes = request.AllowedGrantTypes;
            var grants = ParseJsonArray(request.AllowedGrantTypes);
            descriptor.Permissions.Clear();
            descriptor.Permissions.Add("ep:token");
            descriptor.Permissions.Add("ep:authorization");
            descriptor.Permissions.Add("ep:logout");
            descriptor.Permissions.Add("ep:userinfo");
            foreach (var grant in grants)
            {
                descriptor.Permissions.Add(grant switch
                {
                    "authorization_code" => "gt:authorization_code",
                    "password" => "gt:password",
                    "refresh_token" => "gt:refresh_token",
                    "client_credentials" => "gt:client_credentials",
                    _ => throw new InvalidOperationException($"Grant type non supporté : {grant}")
                });
            }
            descriptor.Permissions.Add("scp:openid");
            descriptor.Permissions.Add("scp:email");
            descriptor.Permissions.Add("scp:profile");
            descriptor.Permissions.Add("scp:roles");
            descriptor.Permissions.Add("scp:offline_access");
            descriptor.Permissions.Add("scp:api");
            needOpenIddictUpdate = true;
        }

        tenantClient.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (needOpenIddictUpdate)
            await _applicationManager.UpdateAsync(app, descriptor);

        return MapToResponse(tenantClient);
    }

    public async Task DeleteClientAsync(Guid tenantId, Guid clientId)
    {
        var tenantClient = await _db.TenantClients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId)
            ?? throw new KeyNotFoundException("Client OAuth non trouvé");

        var app = await _applicationManager.FindByIdAsync(tenantClient.ApplicationId);
        if (app != null)
            await _applicationManager.DeleteAsync(app);

        _db.TenantClients.Remove(tenantClient);
        await _db.SaveChangesAsync();
    }

    private static TenantClientResponse MapToResponse(TenantClient c)
    {
        return new TenantClientResponse(
            c.Id, c.TenantId, c.ClientId, c.ClientName, c.Description,
            DeserializeJsonArray(c.RedirectUris),
            DeserializeJsonArray(c.PostLogoutRedirectUris),
            DeserializeJsonArray(c.AllowedGrantTypes),
            c.IsActive, c.CreatedAt);
    }

    private static List<string> ParseJsonArray(string? json) =>
        string.IsNullOrWhiteSpace(json) ? [] :
        JsonSerializer.Deserialize<List<string>>(json) ?? [];

    private static List<string> ParseJsonArrayOrDefault(string? json, string[] defaults) =>
        string.IsNullOrWhiteSpace(json) ? [.. defaults] : ParseJsonArray(json);

    private static List<string> DeserializeJsonArray(string json) =>
        JsonSerializer.Deserialize<List<string>>(json) ?? [];

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
