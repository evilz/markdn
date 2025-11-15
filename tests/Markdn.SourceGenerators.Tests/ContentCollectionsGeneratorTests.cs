using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Markdn.SourceGenerators.Tests;

public class ContentCollectionsGeneratorTests
{
    private const string ModelSource = @"
using System;
using Markdn.Content;

namespace TestNamespace;

[Collection(""Content/Posts/*.md"", Name = ""Posts"")]
public class Post
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime PubDate { get; set; }
    public string? Description { get; set; }
}";

    [Fact]
    public void Generator_WithAnnotatedModel_GeneratesService()
    {
        var (compilation, additionalTexts, optionsProvider, projectDir) = CreateTestEnvironment(ModelSource);
        try
        {
            var generator = new ContentCollectionsGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: ImmutableArray.Create<ISourceGenerator>(generator.AsSourceGenerator()),
                optionsProvider: optionsProvider);

            driver = driver.AddAdditionalTexts(additionalTexts);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            var runResult = driver.GetRunResult();
            var generatedSources = runResult.Results.SelectMany(r => r.GeneratedSources).ToList();
            var serviceSources = generatedSources
                .Where(source => source.HintName == "Collections.Posts.g.cs")
                .ToList();
            Assert.NotEmpty(serviceSources);

            var serviceCode = serviceSources[0].SourceText.ToString();
            Assert.Contains("public interface IPostsService", serviceCode);
            Assert.Contains("List<global::TestNamespace.Post>", serviceCode);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void Generator_WithoutAnnotatedModel_DoesNotProduceSources()
    {
        const string source = "namespace TestNamespace; public class PlainModel {}";
        var (compilation, additionalTexts, optionsProvider, projectDir) = CreateTestEnvironment(source);
        try
        {
            var generator = new ContentCollectionsGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: ImmutableArray.Create<ISourceGenerator>(generator.AsSourceGenerator()),
                optionsProvider: optionsProvider);

            driver = driver.AddAdditionalTexts(additionalTexts);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            var generatedSources = driver.GetRunResult().Results.SelectMany(r => r.GeneratedSources).ToList();
            Assert.All(generatedSources, source => Assert.Equal("CollectionAttribute.g.cs", source.HintName));
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void Generator_AddsDateTimeParsingForMatchingProperties()
    {
        const string source = @"
using System;
using Markdn.Content;

namespace TestNamespace;

[Collection(""Content/Posts/*.md"", Name = ""Posts"")]
public class Post
{
    public string Slug { get; set; } = string.Empty;
    public DateTime PubDate { get; set; }
}";

        var (compilation, additionalTexts, optionsProvider, projectDir) = CreateTestEnvironment(source);
        try
        {
            var generator = new ContentCollectionsGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: ImmutableArray.Create<ISourceGenerator>(generator.AsSourceGenerator()),
                optionsProvider: optionsProvider);

            driver = driver.AddAdditionalTexts(additionalTexts);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            var generatedSources = driver.GetRunResult().Results.SelectMany(r => r.GeneratedSources).ToList();
            var serviceSources = generatedSources
                .Where(source => source.HintName.StartsWith("Collections.", StringComparison.Ordinal))
                .ToList();
            Assert.NotEmpty(serviceSources);

            var serviceCode = serviceSources[0].SourceText.ToString();
            Assert.Contains("DateTime.TryParse", serviceCode);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    private static (Compilation Compilation, ImmutableArray<AdditionalText> AdditionalTexts, AnalyzerConfigOptionsProvider OptionsProvider, string ProjectDirectory)
        CreateTestEnvironment(string source)
    {
        var projectDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
        var markdownPath = Path.Combine(projectDir, "Content", "Posts", "first-post.md");
        Directory.CreateDirectory(Path.GetDirectoryName(markdownPath)!);

        var markdownContent = @"---
title: ""Hello""
pubDate: 2024-01-01
description: ""Sample""
---";

        var additionalText = new TestAdditionalText(markdownPath, markdownContent);
        var additionalTexts = ImmutableArray.Create<AdditionalText>(additionalText);
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(projectDir, "TestNamespace", additionalTexts);

        var compilation = CreateCompilation(source);
        return (compilation, additionalTexts, optionsProvider, projectDir);
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        return CSharpCompilation.Create("TestCompilation",
            new[] { syntaxTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private sealed class TestAdditionalText : AdditionalText
    {
        private readonly string _content;

        public TestAdditionalText(string path, string content)
        {
            Path = path;
            _content = content;
        }

        public override string Path { get; }

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
        {
            return SourceText.From(_content, Encoding.UTF8);
        }
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _globalOptions;
        private readonly Dictionary<string, AnalyzerConfigOptions> _fileOptions;

        public TestAnalyzerConfigOptionsProvider(string projectDir, string rootNamespace, ImmutableArray<AdditionalText> additionalTexts)
        {
            _globalOptions = new TestAnalyzerConfigOptions(new Dictionary<string, string>
            {
                ["build_property.projectdir"] = projectDir,
                ["build_property.RootNamespace"] = rootNamespace
            });

            _fileOptions = additionalTexts.ToDictionary(
                text => text.Path,
                text =>
                {
                    var relative = Path.GetRelativePath(projectDir, text.Path).Replace('\\', '/');
                    return (AnalyzerConfigOptions)new TestAnalyzerConfigOptions(new Dictionary<string, string>
                    {
                        ["build_metadata.AdditionalFiles.RelativePath"] = relative
                    });
                });
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestAnalyzerConfigOptions.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText text) =>
            _fileOptions.TryGetValue(text.Path, out var options) ? options : TestAnalyzerConfigOptions.Empty;
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public static readonly AnalyzerConfigOptions Empty = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        private readonly IReadOnlyDictionary<string, string> _values;

        public TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
        {
            _values = values;
        }

        public override bool TryGetValue(string key, out string value)
        {
            if (_values.TryGetValue(key, out var stored))
            {
                value = stored;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
