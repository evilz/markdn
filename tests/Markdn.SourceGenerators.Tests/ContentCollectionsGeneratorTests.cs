using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Markdn.SourceGenerators.Tests;

public class ContentCollectionsGeneratorTests
{
    [Fact]
    public void Generator_WithValidCollectionsJson_GeneratesCode()
    {
        // Arrange
        var collectionsJson = @"{
  ""collections"": {
    ""posts"": {
      ""folder"": ""Content/posts"",
      ""schema"": {
        ""type"": ""object"",
        ""properties"": {
          ""title"": {
            ""type"": ""string""
          },
          ""pubDate"": {
            ""type"": ""string"",
            ""format"": ""date-time""
          }
        },
        ""required"": [""title"", ""pubDate""]
      }
    }
  }
}";

        var compilation = CreateCompilation();
        var generator = new ContentCollectionsGenerator();

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(
            new TestAdditionalText("collections.json", collectionsJson)
        ));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var result = driver.GetRunResult();
        var generatedTrees = result.GeneratedTrees;
        Assert.NotEmpty(generatedTrees);

        // Verify generated files
        var generatedFiles = result.Results[0].GeneratedSources;
        
        // Debug: print what we got
        var hintNames = string.Join(", ", generatedFiles.Select(s => s.HintName));
        
        // At minimum, we should have generated something
        Assert.True(generatedFiles.Length >= 2, $"Expected at least 2 files, got {generatedFiles.Length}. Files: {hintNames}");
    }

    [Fact]
    public void Generator_WithoutCollectionsJson_DoesNotGenerateCode()
    {
        // Arrange
        var compilation = CreateCompilation();
        var generator = new ContentCollectionsGenerator();

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // Assert
        var generatedTrees = driver.GetRunResult().GeneratedTrees;
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void Generator_ParsesDateTimeFormat_Correctly()
    {
        // Arrange
        var collectionsJson = @"{
  ""collections"": {
    ""posts"": {
      ""folder"": ""posts"",
      ""schema"": {
        ""properties"": {
          ""pubDate"": {
            ""type"": ""string"",
            ""format"": ""date-time""
          }
        }
      }
    }
  }
}";

        var compilation = CreateCompilation();
        var generator = new ContentCollectionsGenerator();

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(
            new TestAdditionalText("collections.json", collectionsJson)
        ));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // Assert
        var result = driver.GetRunResult();
        var generatedFiles = result.Results[0].GeneratedSources;
        
        // Should generate at least one file
        Assert.True(generatedFiles.Length > 0);
        
        // Check if DateTime is used somewhere in generated code
        var allCode = string.Join("\n", generatedFiles.Select(f => f.SourceText.ToString()));
        Assert.Contains("DateTime", allCode);
    }

    private static Compilation CreateCompilation()
    {
        return CSharpCompilation.Create("TestCompilation",
            new[] { CSharpSyntaxTree.ParseText("") },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
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

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
        {
            return SourceText.From(_text, System.Text.Encoding.UTF8);
        }
    }
}
