using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DotnetNiger.Identity.Domain.Entities;

namespace DotnetNiger.Identity.IntegrationTests;

public static class TestUserFactory
{
    public static async Task<string> CreateUserTokenAsync(
        IServiceProvider services,
        string email,
        string username,
        string password,
        string? role = null)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var user = new ApplicationUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException(message);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var exists = await roleManager.RoleExistsAsync(role);
            if (!exists)
                await roleManager.CreateAsync(new ApplicationRole { Name = role });

            await userManager.AddToRoleAsync(user, role);
        }

        return "placeholder-token";
    }
}
