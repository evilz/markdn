using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Markdn.SourceGenerators.Tests;

public class MarkdownComponentGeneratorTests
{
    [Fact]
    public void Generator_WithSlug_AddsRouteAttribute()
    {
        var markdown = @"---
slug: blog/hello-world
---
# Hello World";

        var tempRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MarkdnTests", Guid.NewGuid().ToString());
        var filePath = System.IO.Path.Combine(tempRoot, "Pages", "blog", "hello-world.md");

        try
        {
            var additionalText = new TestAdditionalText(filePath, markdown);

            var compilation = CSharpCompilation.Create(
                "MarkdownTest",
                new[] { CSharpSyntaxTree.ParseText("") },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new MarkdownComponentGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver
                .Create(generator)
                .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalText));

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            var runResult = driver.GetRunResult();
            var generatedSources = runResult.Results.SelectMany(r => r.GeneratedSources).ToList();
            var generatedCode = string.Join("\n", generatedSources.Select(s => s.SourceText.ToString()));
            if (generatedSources.Count == 0)
            {
                var diagnostics = string.Join(Environment.NewLine,
                    runResult.Diagnostics.Select(d => $"{d.Id}:{d.GetMessage()}"));
                throw new Xunit.Sdk.XunitException("No generated sources. Diagnostics: " + diagnostics);
            }

            Assert.Contains("[Microsoft.AspNetCore.Components.RouteAttribute(\"/blog/hello-world\")]", generatedCode);
        }
        finally
        {
            if (System.IO.Directory.Exists(tempRoot))
            {
                try
                {
                    System.IO.Directory.Delete(tempRoot, true);
                }
                catch
                {
                    // Ignore exceptions during cleanup
                }
            }
        }
    }


    private static GeneratorDriver CreateDriver(params AdditionalText[] additionalTexts)
    {
        var generator = new MarkdownComponentGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: Array.Empty<SyntaxTree>(),
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Components.ComponentBase).Assembly
                    .Location)
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return driver.AddAdditionalTexts(ImmutableArray.Create(additionalTexts))
            .RunGenerators(compilation);
    }

    [Fact]
    public void Initialize_ShouldGenerateSourceForMarkdownFile()
    {
        // Arrange
        var markdown = "# Hello World\n\nThis is a test.";
        var additionalText = new TestAdditionalText("test.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedTrees);
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("class Test", generatedSource);
        Assert.Contains("BuildRenderTree", generatedSource);
    }

    [Fact]
    public void Initialize_ShouldIgnoreNonMarkdownFiles()
    {
        // Arrange
        var additionalText = new TestAdditionalText("test.txt", "Some content");

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        Assert.Empty(result.GeneratedTrees);
    }

    [Fact]
    public void Initialize_ShouldParseYamlFrontMatter()
    {
        // Arrange
        var markdown = @"---
    title: Test Page
    slug: /test
    ---
    # Content";
        var additionalText = new TestAdditionalText("test.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        Assert.Empty(result.Diagnostics);
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("@page \"/test\"", generatedSource);
    }

    [Fact]
    public void Initialize_ShouldReportInvalidYamlFrontMatter()
    {
        // Arrange
        var markdown = @"---
    title: Test
    invalid: [unclosed
    ---
    # Content";
        var additionalText = new TestAdditionalText("test.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "MD102");
    }

    [Fact]
    public void Initialize_ShouldValidateParameterNames()
    {
        // Arrange
        var markdown = @"---
    parameters:
      - name: invalid-name
        type: string
    ---
    # Content";
        var additionalText = new TestAdditionalText("test.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "MD085");
    }

    [Fact]
    public void Initialize_ShouldDetectDuplicateParameterNames()
    {
        // Arrange
        var markdown = @"---
    parameters:
      - name: Title
        type: string
      - name: Title
        type: int
    ---
    # Content";
        var additionalText = new TestAdditionalText("test.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "MD087");
    }

    [Fact]
    public void Initialize_ShouldValidateParameterTypes()
    {
        // Arrange
        var markdown = @"---
    parameters:
      - name: Value
        type: 123InvalidType
    ---
    # Content";
        var additionalText = new TestAdditionalText("test.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "MD086");
    }

    [Fact]
    public void Initialize_ShouldPreserveRazorSyntax()
    {
        // Arrange
        var markdown = "# Hello @Name\n\n@if (ShowContent) { <p>Content</p> }";
        var additionalText = new TestAdditionalText("test.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("@Name", generatedSource);
        Assert.Contains("@if", generatedSource);
    }

    [Fact]
    public void Initialize_ShouldReportMalformedRazorSyntax()
    {
        // Arrange
        var markdown = "# Content\n\n@if (condition { incomplete";
        var additionalText = new TestAdditionalText("test.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "MD103");
    }

    [Fact]
    public void Initialize_ShouldExtractCodeBlocks()
    {
        // Arrange
        var markdown = @"# Content

    @code {
        private string message = ""Hello"";

        protected override void OnInitialized()
        {
            // Initialization logic
        }
    }";
        var additionalText = new TestAdditionalText("test.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("private string message", generatedSource);
        Assert.Contains("OnInitialized", generatedSource);
    }

    [Fact]
    public void Initialize_ShouldResolveComponentReferences()
    {
        // Arrange
        var componentSource = @"
    namespace TestAssembly.Components
    {
        public class MyComponent : Microsoft.AspNetCore.Components.ComponentBase { }
    }";
        var markdown = "<MyComponent />";

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(componentSource) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Components.ComponentBase).Assembly
                    .Location)
            });

        var generator = new MarkdownComponentGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(new TestAdditionalText("test.md", markdown)))
            .RunGenerators(compilation);

        var result = driver.GetRunResult();

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("TestAssembly.Components", generatedSource);
    }

    [Fact]
    public void Initialize_ShouldReportUnresolvableComponentReference()
    {
        // Arrange
        var markdown = "<UnknownComponent />";
        var additionalText = new TestAdditionalText("test.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "MD006");
    }

    [Fact]
    public void Initialize_ShouldGenerateRouteFromSlug()
    {
        // Arrange
        var markdown = @"---
    slug: /my-custom-route
    ---
    # Content";
        var additionalText = new TestAdditionalText("pages/test.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("@page \"/my-custom-route\"", generatedSource);
    }

    [Fact]
    public void Initialize_ShouldDeriveRouteFromFilePath()
    {
        // Arrange
        var markdown = "# Content";
        var additionalText = new TestAdditionalText("pages/blog/post.md", markdown);

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("@page \"/blog/post\"", generatedSource);
    }

    [Fact]
    public void Initialize_ShouldHandleExceptions()
    {
        // Arrange
        var additionalText = new ThrowingAdditionalText("test.md");

        // Act
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "MD999" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Initialize_ShouldUseRootNamespaceFromAnalyzerConfig()
    {
        // Arrange
        var markdown = "# Content";
        var additionalText = new TestAdditionalText("test.md", markdown);
        var configOptions = new TestAnalyzerConfigOptions(
            new Dictionary<string, string> { ["build_property.RootNamespace"] = "MyCustomNamespace" });

        var generator = new MarkdownComponentGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalText))
            .WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(configOptions))
            .RunGenerators(CreateMinimalCompilation());

        var result = driver.GetRunResult();

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("namespace MyCustomNamespace", generatedSource);
    }

    private static Compilation CreateMinimalCompilation()
    {
        return CSharpCompilation.Create(
            "TestAssembly",
            Array.Empty<SyntaxTree>(),
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Components.ComponentBase).Assembly
                    .Location)
            });
    }
}

// Classes utilitaires pour les tests

internal class TestAdditionalText : AdditionalText
{
    private readonly string _text;

    public TestAdditionalText(string path, string text)
    {
        Path = path;
        _text = text;
    }

    public override string Path { get; }

    public override SourceText GetText(CancellationToken cancellationToken = default)
        => SourceText.From(_text, Encoding.UTF8);
}

internal class ThrowingAdditionalText : AdditionalText
{
    public ThrowingAdditionalText(string path)
    {
        Path = path;
    }

    public override string Path { get; }

    public override SourceText GetText(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Simulated error");
}

internal class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _options;

    public TestAnalyzerConfigOptions(Dictionary<string, string> options)
    {
        _options = options;
    }

    public override bool TryGetValue(string key, out string value)
        => _options.TryGetValue(key, out value!);
}

internal class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly AnalyzerConfigOptions _globalOptions;

    public TestAnalyzerConfigOptionsProvider(AnalyzerConfigOptions globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        => _globalOptions;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        => _globalOptions;
}
