using Markdn.Api.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Markdn.Api.Tests.Contract;

/// <summary>
/// Custom WebApplicationFactory for contract tests that configures test content directory
/// </summary>
public class ContractTestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Get the test content directory path
            var testContentDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "TestContent"
            );

            // Ensure directory exists
            if (!Directory.Exists(testContentDir))
            {
                Directory.CreateDirectory(testContentDir);
            }

            // Attempt to locate the repository's TestContent source directory (next to the test project)
            // and copy its files into the runtime TestContent directory so the test host can read fixtures.
            try
            {
                var sourceTestContent = FindSourceTestContentDirectory();
                if (!string.IsNullOrEmpty(sourceTestContent) && Directory.Exists(sourceTestContent))
                {
                    CopyDirectoryRecursively(sourceTestContent, testContentDir);
                }
            }
            catch
            {
                // Swallow any IO errors here; tests will fail later if fixtures are missing.
            }
            // Override configuration to use test content directory
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Markdn:ContentDirectory"] = testContentDir,
                ["Markdn:EnableFileWatching"] = "false"
            });
        });

        base.ConfigureWebHost(builder);
    }

    private static string? FindSourceTestContentDirectory()
    {
        // Start from the assembly base directory and walk upwards looking for a TestContent folder next to the test project
        var dir = AppContext.BaseDirectory;

        for (int i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "TestContent");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            var parent = Directory.GetParent(dir);
            if (parent == null)
            {
                break;
            }
            dir = parent.FullName;
        }

        return null;
    }

    private static void CopyDirectoryRecursively(string sourceDir, string targetDir)
    {
        foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourceDir, targetDir));
        }

        foreach (var newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourceDir, targetDir), overwrite: true);
        }
    }
}
