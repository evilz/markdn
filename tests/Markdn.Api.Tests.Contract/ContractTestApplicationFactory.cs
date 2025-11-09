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

            // Override configuration to use test content directory
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Markdn:ContentDirectory"] = testContentDir,
                ["Markdn:EnableFileWatching"] = "false"
            });
        });

        base.ConfigureWebHost(builder);
    }
}
