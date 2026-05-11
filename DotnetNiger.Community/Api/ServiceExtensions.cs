using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Api;

public static class ServiceExtensions
{
    public static IServiceCollection AddCommunityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }

    public static IServiceCollection AddCommunityServices(this IServiceCollection services)
    {
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }

    public static IServiceCollection AddCommunityAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Jwt:Authority"] ?? "http://localhost:5075";
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
                options.MetadataAddress = configuration["Jwt:MetadataAddress"] ?? "http://localhost:5075/.well-known/openid-configuration";
                options.RequireHttpsMetadata = false;
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddCommunityHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IIdentityApiClient, IdentityApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Identity:BaseUrl"] ?? "http://localhost:5075");
        });

        return services;
    }
}
