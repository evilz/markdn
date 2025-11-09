using FluentAssertions;
using Markdn.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Markdn.Api.Tests.Integration;

public class ContentRenderingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testContentDir;

    public ContentRenderingTests(WebApplicationFactory<Program> factory)
    {
        _testContentDir = Path.Combine(Path.GetTempPath(), "markdn-rendering-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_testContentDir);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Markdn:ContentDirectory"] = _testContentDir,
                    ["Markdn:EnableFileWatching"] = "false"
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task MarkdownWithCodeBlocks_ShouldRenderCorrectlyToHtml()
    {
        // Arrange
        var markdownContent = @"---
title: Code Examples
---
# Code Samples

Here is some inline `code` in a sentence.

```csharp
public class Example
{
    public string Name { get; set; }
}
```

And another block:

```javascript
const hello = () => {
    console.log('Hello, World!');
};
```
";

        var testFile = Path.Combine(_testContentDir, "code-examples.md");
        await File.WriteAllTextAsync(testFile, markdownContent);

        // Give time for repository to pick up file
        await Task.Delay(500);

        // Act
        var response = await _client.GetAsync("/api/content/code-examples?format=html");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentItemResponse>();
        content.Should().NotBeNull();
        content!.HtmlContent.Should().NotBeNullOrEmpty();
        
        // Verify code blocks are rendered with <pre><code> tags
        content.HtmlContent.Should().Contain("<pre><code");
        content.HtmlContent.Should().Contain("class=\"language-csharp\"");
        content.HtmlContent.Should().Contain("class=\"language-javascript\"");
        content.HtmlContent.Should().Contain("public class Example");
        content.HtmlContent.Should().Contain("const hello");
        
        // Verify inline code
        content.HtmlContent.Should().Contain("<code>code</code>");

        // Cleanup
        Directory.Delete(_testContentDir, true);
    }

    [Fact]
    public async Task GfmTables_ShouldRenderCorrectlyToHtml()
    {
        // Arrange
        var markdownContent = @"---
title: Table Example
---
# Data Tables

## Features Table

| Feature | Status | Priority |
|---------|--------|----------|
| Markdown | ✓ Done | High |
| HTML | ✓ Done | High |
| Tables | ✓ Done | Medium |
| Code Blocks | ✓ Done | Medium |

## Alignment Test

| Left | Center | Right |
|:-----|:------:|------:|
| A    | B      | C     |
| 1    | 2      | 3     |
";

        var testFile = Path.Combine(_testContentDir, "table-example.md");
        await File.WriteAllTextAsync(testFile, markdownContent);

        // Give time for repository to pick up file
        await Task.Delay(500);

        // Act
        var response = await _client.GetAsync("/api/content/table-example?format=html");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentItemResponse>();
        content.Should().NotBeNull();
        content!.HtmlContent.Should().NotBeNullOrEmpty();
        
        // Verify table structure
        content.HtmlContent.Should().Contain("<table>");
        content.HtmlContent.Should().Contain("<thead>");
        content.HtmlContent.Should().Contain("<tbody>");
        content.HtmlContent.Should().Contain("<th>Feature</th>");
        content.HtmlContent.Should().Contain("<th>Status</th>");
        content.HtmlContent.Should().Contain("<th>Priority</th>");
        
        // Verify table data
        content.HtmlContent.Should().Contain("<td>Markdown</td>");
        content.HtmlContent.Should().Contain("<td>✓ Done</td>");
        content.HtmlContent.Should().Contain("<td>High</td>");

        // Verify alignment (Markdig should add style or class for alignment)
        content.HtmlContent.Should().MatchRegex("<th.*?>Left</th>"); // Left aligned
        content.HtmlContent.Should().MatchRegex("<th.*?>Center</th>"); // Center aligned
        content.HtmlContent.Should().MatchRegex("<th.*?>Right</th>"); // Right aligned

        // Cleanup
        Directory.Delete(_testContentDir, true);
    }
}
