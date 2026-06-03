using DotnetNiger.Gateway.Configuration;
using DotnetNiger.Gateway.HealthChecks;
using DotnetNiger.Gateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Polly;
using Ocelot.Provider.Consul;

namespace DotnetNiger.Gateway.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddOcelot(configuration)
            .AddCacheManager(x => x.WithDictionaryHandle())
            .AddPolly()
            .AddConsul();

        services.AddHttpClient();
        services.AddMemoryCache();
        services.AddHostedService<ExternalServiceHealthService>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerForOcelot(configuration);

        services.AddHealthChecks()
            .AddCheck<SmtpHealthCheck>("smtp", HealthStatus.Degraded, ["infrastructure", "email"])
            .AddCheck<DownstreamHealthCheck>("downstream", HealthStatus.Degraded, ["gateway", "connectivity"]);

        services.AddCors(options =>
        {
            if (environment.IsDevelopment())
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            else
            {
                var origins = configuration["Cors:AllowedOrigins"];
                if (!string.IsNullOrWhiteSpace(origins))
                    options.AddPolicy("AllowAll", policy =>
                        policy.WithOrigins(origins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                              .AllowAnyMethod().AllowAnyHeader().AllowCredentials());
                else
                {
                    var identityUrl = (configuration["DeveloperPortal:IdentityBaseUrl"] ?? "http://localhost:5075").TrimEnd('/');
                    options.AddPolicy("AllowAll", policy =>
                        policy.WithOrigins(identityUrl)
                              .AllowAnyMethod().AllowAnyHeader().AllowCredentials());
                }
            }
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var identityUrl = (configuration["DeveloperPortal:IdentityBaseUrl"] ?? "http://localhost:5075").TrimEnd('/');
                options.Authority = identityUrl;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = identityUrl,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning(context.Exception, "JWT authentication failed");
                        return Task.CompletedTask;
                    }
                };
            });

        services.Configure<List<DownstreamServiceConfig>>(configuration.GetSection("DownstreamServices"));

        return services;
    }


}
