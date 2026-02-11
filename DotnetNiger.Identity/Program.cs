// Composant Identity: Program
using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using DotnetNiger.Identity.Api.Extensions;
using DotnetNiger.Identity.Api.Filters;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using DotnetNiger.Identity.Infrastructure.External;
using DotnetNiger.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
// Configuration Serilog avec sinks definis dans appsettings.
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());
// Configuration principale du service Identity.
var connectionString = builder.Configuration.GetConnectionString("DotnetNigerIdentityDbContext") ?? throw new InvalidOperationException("Connection string 'DotnetNigerIdentityContextConnection' not found.");

// Chargement de la configuration JWT.
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration section 'Jwt' not found.");

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Gestion centralisee des erreurs metier.
    options.Filters.Add<ExceptionFilter>();
    options.Filters.Add<ValidateModelFilter>();
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    // Format de groupe: v1, v1.0, etc.
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddDbContext<DotnetNigerIdentityDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<Role>()
    .AddEntityFrameworkStores<DotnetNigerIdentityDbContext>()
    .AddSignInManager<SignInManager<ApplicationUser>>()
    .AddDefaultTokenProviders();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<FileUploadOptions>(builder.Configuration.GetSection("FileUpload"));
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<RefreshTokenGenerator>();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddEmailProviders();
builder.Services.AddIdentityApplicationServices();
builder.Services.AddHostedService<AvatarCleanupService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Smart";
        options.DefaultChallengeScheme = "Smart";
    })
    .AddPolicyScheme("Smart", "Smart", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.ContainsKey("X-API-Key")
                ? "ApiKey"
                : JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKeyOnly", policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim("scope", "api_key"));
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

var app = builder.Build();

var fileUploadOptions = app.Services.GetRequiredService<IOptions<FileUploadOptions>>().Value;
if (string.Equals(fileUploadOptions.Provider, "Local", StringComparison.OrdinalIgnoreCase))
{
    var uploadRoot = Path.Combine(app.Environment.ContentRootPath, fileUploadOptions.RootPath);
    Directory.CreateDirectory(uploadRoot);
    var publicBasePath = string.IsNullOrWhiteSpace(fileUploadOptions.PublicBasePath)
        ? "/uploads"
        : fileUploadOptions.PublicBasePath;
    if (!publicBasePath.StartsWith('/'))
    {
        publicBasePath = "/" + publicBasePath;
    }

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadRoot),
        RequestPath = publicBasePath
    });
}

// await SeedAdminAsync(app); //appel de la fonction pour cree l'admin

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"Identity Service {description.GroupName}");
        }

        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Identity Service - API Documentation";
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.EnableDeepLinking();
        options.EnableFilter();
    });
}

app.UseHttpsRedirection();
app.UseErrorHandling();
// Journalisation structuree des requetes HTTP.
app.UseSerilogRequestLogging();
app.UseRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program
{
}

// creation de l'admin
// static async Task SeedAdminAsync(WebApplication app)
// {
//     using var scope = app.Services.CreateScope();
//     var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
//     var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//     var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
//     var seedAdminEnabled = config.GetValue("SEED_ADMIN", true);
//     if (!seedAdminEnabled)
//     {
//         return;
//     }

//     const string adminRoleName = "Admin";
//     var adminEmail = config["ADMIN_EMAIL"] ?? "admin@dotnetniger.com";
//     var adminPassword = config["ADMIN_PASSWORD"] ?? "AdminPassword@2006";
//     var adminUsername = config["ADMIN_USERNAME"] ?? "admin";

//     var roleExists = await roleManager.RoleExistsAsync(adminRoleName);
//     if (!roleExists)
//     {
//         await roleManager.CreateAsync(new Role(adminRoleName));
//     }

//     var adminUser = await userManager.FindByEmailAsync(adminEmail);
//     if (adminUser == null)
//     {
//         adminUser = new ApplicationUser
//         {
//             UserName = adminUsername,
//             Email = adminEmail,
//             EmailConfirmed = true,
//             IsActive = true
//         };

//         var createResult = await userManager.CreateAsync(adminUser, adminPassword);
//         if (!createResult.Succeeded)
//         {
//             return;
//         }
//     }

//     var isInRole = await userManager.IsInRoleAsync(adminUser, adminRoleName);
//     if (!isInRole)
//     {
//         await userManager.AddToRoleAsync(adminUser, adminRoleName);
//     }
// }
