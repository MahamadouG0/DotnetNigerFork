using DotnetNiger.Community.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync())
            return;

        var categories = new List<Category>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Développement Web",
                Slug = "développement-web",
                Description = "Tout sur le développement web, du frontend au backend.",
                PostCount = 0
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Mobile",
                Slug = "mobile",
                Description = "Développement d'applications mobiles natives et hybrides.",
                PostCount = 0
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Data & IA",
                Slug = "data-&-ia",
                Description = "Data science, intelligence artificielle et machine learning.",
                PostCount = 0
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "DevOps",
                Slug = "devops",
                Description = "Pratiques DevOps, CI/CD et infrastructure as code.",
                PostCount = 0
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Communauté",
                Slug = "communauté",
                Description = "Vie de la communauté DotnetNiger, événements et actualités.",
                PostCount = 0
            }
        };

        var tags = new List<Tag>
        {
            new() { Id = Guid.NewGuid(), Name = "csharp", Slug = "csharp", UsageCount = 0 },
            new() { Id = Guid.NewGuid(), Name = "dotnet", Slug = "dotnet", UsageCount = 0 },
            new() { Id = Guid.NewGuid(), Name = "javascript", Slug = "javascript", UsageCount = 0 },
            new() { Id = Guid.NewGuid(), Name = "python", Slug = "python", UsageCount = 0 },
            new() { Id = Guid.NewGuid(), Name = "react", Slug = "react", UsageCount = 0 },
            new() { Id = Guid.NewGuid(), Name = "azure", Slug = "azure", UsageCount = 0 },
            new() { Id = Guid.NewGuid(), Name = "sql", Slug = "sql", UsageCount = 0 },
            new() { Id = Guid.NewGuid(), Name = "docker", Slug = "docker", UsageCount = 0 },
            new() { Id = Guid.NewGuid(), Name = "open-source", Slug = "open-source", UsageCount = 0 },
            new() { Id = Guid.NewGuid(), Name = "tutoriel", Slug = "tutoriel", UsageCount = 0 }
        };

        db.Categories.AddRange(categories);
        db.Tags.AddRange(tags);

        var projects = new List<Project>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Plateforme e-commerce DotnetNiger",
                Slug = "plateforme-e-commerce-dotnetniger",
                Description = "Une plateforme e-commerce complète bâtie avec Blazor et .NET 9, intégrant des paiements Mobile Money et une gestion d'inventaire temps réel.",
                Url = "https://github.com/dotnetniger/ecommerce",
                GithubUrl = "https://github.com/dotnetniger/ecommerce",
                Technologies = "Blazor,.NET 9,Entity Framework,PostgreSQL",
                Status = "active",
                CreatedBy = Guid.Empty,
                AuthorName = "Communauté DotnetNiger",
                IsFeatured = true,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "API Gateway DotnetNiger",
                Slug = "api-gateway-dotnetniger",
                Description = "Passerelle API centralisée avec Ocelot, rate limiting, cache Swagger et monitoring des performances par endpoint.",
                Url = "https://github.com/dotnetniger/gateway",
                GithubUrl = "https://github.com/dotnetniger/gateway",
                Technologies = "Ocelot,.NET 9,Swagger,Prometheus",
                Status = "active",
                CreatedBy = Guid.Empty,
                AuthorName = "Communauté DotnetNiger",
                IsFeatured = true,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Application Mobile Meetup",
                Slug = "application-mobile-meetup",
                Description = "Application mobile .NET MAUI pour la gestion des meetups DotnetNiger avec notifications push et QR code pour les inscriptions.",
                Url = "https://github.com/dotnetniger/meetup-app",
                GithubUrl = "https://github.com/dotnetniger/meetup-app",
                Technologies = ".NET MAUI,C#,SignalR,Azure",
                Status = "active",
                CreatedBy = Guid.Empty,
                AuthorName = "Communauté DotnetNiger",
                IsFeatured = false,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        db.Projects.AddRange(projects);

        var partners = new List<Partner>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Microsoft Niger",
                Slug = "microsoft-niger",
                Description = "Partenaire technologique officiel, supportant la communauté avec des ressources Azure et des formations.",
                LogoUrl = "https://img.icons8.com/color/96/microsoft.png",
                WebsiteUrl = "https://www.microsoft.com",
                PartnerType = "sponsor",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Orange Niger",
                Slug = "orange-niger",
                Description = "Opérateur télécom partenaire, facilitant l'organisation des événements et meetups.",
                LogoUrl = "https://img.icons8.com/color/96/orange.png",
                WebsiteUrl = "https://www.orange.ne",
                PartnerType = "sponsor",
                SortOrder = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-50)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Sonatel",
                Slug = "sonatel",
                Description = "Fournisseur d'accès internet et services cloud pour la communauté.",
                LogoUrl = "https://img.icons8.com/color/96/internet.png",
                WebsiteUrl = "https://www.sonatel.sn",
                PartnerType = "partner",
                SortOrder = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            }
        };

        db.Partners.AddRange(partners);

        var newsletterDemo = new NewsletterSubscription
        {
            Id = Guid.NewGuid(),
            Email = "demo@dotnetniger.com",
            Name = "Membre Demo",
            IsActive = true,
            UnsubscribeToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
            SubscribedAt = DateTime.UtcNow.AddDays(-7)
        };

        db.NewsletterSubscriptions.Add(newsletterDemo);

        await db.SaveChangesAsync();
    }
}
