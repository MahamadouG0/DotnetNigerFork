using System.Text;
using Xunit;

namespace DotnetNiger.Architecture.Tests;

public class ApplicationLayerDependencyGuardsTests
{
    private static readonly string[] ForbiddenPatterns =
    [
        ".Api",
        ".Infrastructure.Repositories",
    ];

    [Fact]
    public void CommunityApplication_MustNotReference_ForbiddenNamespaces()
    {
        var applicationPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../DotnetNiger.Community/Application"));

        Assert.True(Directory.Exists(applicationPath), $"Application folder not found: {applicationPath}");

        var files = Directory.GetFiles(applicationPath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file, Encoding.UTF8);
            foreach (var pattern in ForbiddenPatterns)
            {
                if (content.Contains(pattern, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(applicationPath, file)} -> {pattern}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Community Application must not reference forbidden namespaces.\nViolations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void IdentityApplication_MustNotReference_ForbiddenNamespaces()
    {
        var applicationPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../DotnetNiger.Identity/Application"));

        Assert.True(Directory.Exists(applicationPath), $"Application folder not found: {applicationPath}");

        var files = Directory.GetFiles(applicationPath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file, Encoding.UTF8);
            foreach (var pattern in ForbiddenPatterns)
            {
                if (content.Contains(pattern, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(applicationPath, file)} -> {pattern}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Identity Application must not reference forbidden namespaces.\nViolations:\n" + string.Join("\n", violations));
    }
}
