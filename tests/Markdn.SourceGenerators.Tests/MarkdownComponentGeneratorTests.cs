using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
                var diagnostics = string.Join(Environment.NewLine, runResult.Diagnostics.Select(d => $"{d.Id}:{d.GetMessage()}"));
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

    private sealed class TestAdditionalText : AdditionalText
    {
        private readonly string _text;

        public TestAdditionalText(string path, string text)
        {
            Path = path;
            _text = text;
        }

        public override string Path { get; }

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
            => SourceText.From(_text, Encoding.UTF8);
    }
}
