using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetNiger.Community.Api;
using DotnetNiger.Community.Api.Middleware;
using DotnetNiger.Community.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/community-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

    builder.Services.AddProblemDetails();
    builder.Services.AddApiVersioningWithSwagger();
    builder.Services.AddCommunityInfrastructure(builder.Configuration);
    builder.Services.AddCommunityAuthentication(builder.Configuration, builder.Environment);
    builder.Services.AddCommunityServices();
    builder.Services.AddCommunityHttpClients(builder.Configuration);

    builder.Services.AddCors(options =>
    {
        if (builder.Environment.IsDevelopment())
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        else
        {
            var origins = builder.Configuration["Cors:AllowedOrigins"];
            if (!string.IsNullOrWhiteSpace(origins))
                options.AddDefaultPolicy(policy =>
                    policy.WithOrigins(origins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                          .AllowAnyMethod().AllowAnyHeader().AllowCredentials());
            else
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        }
    });

    var app = builder.Build();

    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DotnetNiger Community API v1");
        options.RoutePrefix = "swagger";
    });

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (InvalidOperationException) when (db.Database.GetPendingMigrations().Any())
        {
            db.Database.EnsureCreated();
        }
        await DbSeeder.SeedAsync(db);
    }

    Log.Information("DotnetNiger.Community starting...");
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

public partial class Program { }


