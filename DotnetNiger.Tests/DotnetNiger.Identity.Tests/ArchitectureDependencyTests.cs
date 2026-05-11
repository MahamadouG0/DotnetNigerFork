using System.Text;
using Xunit;

namespace DotnetNiger.Identity.Tests;

public class ArchitectureDependencyTests
{
    [Fact]
    public void IdentityApplication_MustNotReference_IdentityInfrastructureRepositoriesNamespace()
    {
        var applicationPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../DotnetNiger.Identity/Application"));

        Assert.True(Directory.Exists(applicationPath), $"Application folder not found: {applicationPath}");

        var files = Directory.GetFiles(applicationPath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file, Encoding.UTF8);
            if (content.Contains("DotnetNiger.Identity.Infrastructure.Repositories", StringComparison.Ordinal))
            {
                violations.Add(Path.GetRelativePath(applicationPath, file));
            }
        }

        Assert.True(
            violations.Count == 0,
            "Application layer must not reference Infrastructure.Repositories. Violations: " + string.Join(", ", violations));
    }
}
