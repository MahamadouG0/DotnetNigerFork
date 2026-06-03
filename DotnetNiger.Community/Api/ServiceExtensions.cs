using Asp.Versioning;
using DotnetNiger.Community.Application.Notifications;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace DotnetNiger.Community.Api;

public static class ServiceExtensions
{
    public static IServiceCollection AddCommunityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var provider = configuration.GetValue<string>("DatabaseProvider", "Sqlite");
            var connStr = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=DotnetNigerCommunity.db";

            if (provider == "SqlServer")
                options.UseSqlServer(connStr, x => x.MigrationsAssembly("DotnetNiger.Community"));
            else if (provider is "PostgreSql" or "PostgreSQL" or "Npgsql")
                options.UseNpgsql(connStr, x => x.MigrationsAssembly("DotnetNiger.Community"));
            else
                options.UseSqlite(connStr, x => x.MigrationsAssembly("DotnetNiger.Community"));
        });

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
        services.AddScoped<INewsletterService, NewsletterService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IMemberDirectoryService, MemberDirectoryService>();
        services.AddScoped<IPartnerService, PartnerService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    public static IServiceCollection AddCommunityAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Jwt:Authority"] ?? "http://localhost:5075";
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "http://localhost:5075/",
                };
                options.MetadataAddress = configuration["Jwt:MetadataAddress"] ?? "http://localhost:5075/.well-known/openid-configuration";
                options.RequireHttpsMetadata = !environment.IsDevelopment();
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddCommunityHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["Identity:ApiKey"] ?? configuration["Integration:ProvisioningApiKey"] ?? "";

        services.AddHttpClient<IIdentityApiClient, IdentityApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Identity:BaseUrl"] ?? "http://localhost:5075");
            if (!string.IsNullOrEmpty(apiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        });

        return services;
    }

    public static IServiceCollection AddApiVersioningWithSwagger(
        this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DotnetNiger Community API",
                Version = "v1.0",
                Description = "API publique de la communaut\u00e9 DotnetNiger - Posts, Events, Resources, Comments, Profile, Admin"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description = "Entrez le token JWT : Bearer {votre_token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }
}
