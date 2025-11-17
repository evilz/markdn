using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Threading;
using System.Text;
using System.Linq;
using Xunit;

namespace Markdn.SourceGenerators.Tests;

public class RazorGeneratorTests
{
    [Fact]
    public void Should_Generate_Razor_File_With_Page_Directive_From_Slug()
    {
        var markdown = @"---
slug: /blog/hello-world
title: Hello World
---
# Hello World

This is a test.";

        var additionalText = new TestAdditionalText("test.md", markdown);
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Single(result.GeneratedTrees);

        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Should contain @page directive with slug
        Assert.Contains("@page \"/blog/hello-world\"", generatedSource);
        // Should contain PageTitle
        Assert.Contains("<PageTitle>Hello World</PageTitle>", generatedSource);
        // Should contain the HTML content (Markdig adds id attributes to headings)
        Assert.Contains("<h1 id=\"hello-world\">Hello World</h1>", generatedSource);
    }

    [Fact]
    public void Should_Derive_Route_From_File_Path()
    {
        var markdown = @"# Content";

        var additionalText = new TestAdditionalText("pages/blog/post.md", markdown);
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Single(result.GeneratedTrees);

        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Should derive route from file path
        Assert.Contains("@page \"/blog/post\"", generatedSource);
    }

    [Fact]
    public void Should_Parse_Markdown_Using_Markdig()
    {
        var markdown = @"# Heading 1

## Heading 2

- List item 1
- List item 2

**Bold text** and *italic text*.";

        var additionalText = new TestAdditionalText("test.md", markdown);
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Should have proper HTML tags from Markdig (with id attributes)
        Assert.Contains("<h1 id=\"heading-1\">Heading 1</h1>", generatedSource);
        Assert.Contains("<h2 id=\"heading-2\">Heading 2</h2>", generatedSource);
        Assert.Contains("<li>List item 1</li>", generatedSource);
        Assert.Contains("<strong>Bold text</strong>", generatedSource);
        Assert.Contains("<em>italic text</em>", generatedSource);
    }

    [Fact]
    public void Should_Add_Parameters_From_Front_Matter()
    {
        var markdown = @"---
parameters:
  - name: Title
    type: string
  - name: Count
    type: int
---
# Content";

        var additionalText = new TestAdditionalText("test.md", markdown);
        var driver = CreateDriver(additionalText);
        var result = driver.GetRunResult();

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Should have @code block with parameters
        Assert.Contains("@code {", generatedSource);
        Assert.Contains("[Parameter]", generatedSource);
        Assert.Contains("public string Title { get; set; } = default!;", generatedSource);
        Assert.Contains("public int Count { get; set; }", generatedSource);
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
                MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Components.ComponentBase).Assembly.Location)
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return driver.AddAdditionalTexts(ImmutableArray.Create(additionalTexts))
            .RunGenerators(compilation);
    }

    private class TestAdditionalText : AdditionalText
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
}

