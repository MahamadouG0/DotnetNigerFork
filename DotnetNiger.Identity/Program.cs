using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Api;
using DotnetNiger.Identity.Api.Middleware;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .Build())
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddProblemDetails();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddIdentityInfrastructure(builder.Configuration);
    builder.Services.AddIdentityServices();
    builder.Services.AddTransient<IClaimsTransformation, RoleClaimsTransformer>();
    builder.Services.AddApiVersioningWithSwagger();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCors("GatewayOnly");

    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<TenantResolutionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "DotnetNiger Identity v1");
            options.RoutePrefix = "swagger";
        });
    }

    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        service = "DotnetNiger.Identity",
        timestamp = DateTime.UtcNow
    }));

    app.MapControllers();

    // Initialisation de la base de données au démarrage
    using (var scope = app.Services.CreateScope())
    {
        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<
            Microsoft.AspNetCore.Identity.UserManager<DotnetNiger.Identity.Domain.Entities.ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<
            Microsoft.AspNetCore.Identity.RoleManager<DotnetNiger.Identity.Domain.Entities.ApplicationRole>>();

        await DbSeeder.SeedAsync(db, userManager, roleManager, tenantContext);
    }

    Log.Information("DotnetNiger.Identity démarré sur {Urls}", string.Join(", ", app.Urls));
    await app.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
