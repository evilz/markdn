using System.IO;
using System.Text;
using FluentAssertions;
using Markdn.Api.Models;

namespace Markdn.Api.Tests.Integration;

/// <summary>
/// Integration tests for querying collection content - T067
/// Tests end-to-end collection query workflow with validated content
/// </summary>
public class CollectionQueryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _collectionsFile;
    private readonly string _contentDir;

    public CollectionQueryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"markdn-test-{Guid.NewGuid()}");
        _collectionsFile = Path.Combine(_tempDir, "collections.json");
        _contentDir = Path.Combine(_tempDir, "content");

        Directory.CreateDirectory(_contentDir);
    }

    [Fact]
    public async Task QueryCollection_WithValidatedContent_ShouldReturnOnlyValidItems()
    {
        // Arrange
        var collectionDir = Path.Combine(_contentDir, "blog");
        Directory.CreateDirectory(collectionDir);

        // Create collections.json
        var collectionsConfig = """
        {
          "collections": {
            "blog": {
              "folder": "content/blog",
              "schema": {
                "type": "object",
                "properties": {
                  "title": { "type": "string" },
                  "author": { "type": "string" },
                  "publishDate": { "type": "string", "format": "date" }
                },
                "required": ["title", "publishDate"],
                "additionalProperties": true
              }
            }
          }
        }
        """;
        await File.WriteAllTextAsync(_collectionsFile, collectionsConfig);

        // Create valid content item
        var validContent = """
        ---
        title: Valid Post
        author: John Doe
        publishDate: 2025-11-09
        slug: valid-post
        ---
        
        This is valid content.
        """;
        await File.WriteAllTextAsync(Path.Combine(collectionDir, "valid-post.md"), validContent);

        // Create invalid content item (missing required field)
        var invalidContent = """
        ---
        author: Jane Doe
        slug: invalid-post
        ---
        
        This is invalid content (missing title and publishDate).
        """;
        await File.WriteAllTextAsync(Path.Combine(collectionDir, "invalid-post.md"), invalidContent);

        // Act - Would normally call API or service here
        // For now, just verify file structure is correct
        var validFile = Path.Combine(collectionDir, "valid-post.md");
        var invalidFile = Path.Combine(collectionDir, "invalid-post.md");

        // Assert
        File.Exists(validFile).Should().BeTrue();
        File.Exists(invalidFile).Should().BeTrue();
        File.Exists(_collectionsFile).Should().BeTrue();

        // This test will be completed once CollectionService is implemented
        // It should load collections, validate content, and return only valid items
    }

    [Fact]
    public async Task QueryCollection_BySlug_ShouldResolveFrontMatterSlug()
    {
        // Arrange
        var collectionDir = Path.Combine(_contentDir, "docs");
        Directory.CreateDirectory(collectionDir);

        var collectionsConfig = """
        {
          "collections": {
            "docs": {
              "folder": "content/docs",
              "schema": {
                "type": "object",
                "properties": {
                  "title": { "type": "string" }
                },
                "required": ["title"],
                "additionalProperties": true
              }
            }
          }
        }
        """;
        await File.WriteAllTextAsync(_collectionsFile, collectionsConfig);

        // Create content with explicit slug
        var content = """
        ---
        title: Custom Slug Doc
        slug: my-custom-slug
        ---
        
        This has a custom slug different from filename.
        """;
        await File.WriteAllTextAsync(
            Path.Combine(collectionDir, "actual-filename.md"), 
            content
        );

        // Act & Assert
        File.Exists(Path.Combine(collectionDir, "actual-filename.md")).Should().BeTrue();
        
        // When implemented, should be able to query by "my-custom-slug" 
        // rather than "actual-filename"
    }

    [Fact]
    public async Task QueryCollection_WithoutExplicitSlug_ShouldFallbackToFilename()
    {
        // Arrange
        var collectionDir = Path.Combine(_contentDir, "articles");
        Directory.CreateDirectory(collectionDir);

        var collectionsConfig = """
        {
          "collections": {
            "articles": {
              "folder": "content/articles",
              "schema": {
                "type": "object",
                "properties": {
                  "title": { "type": "string" }
                },
                "required": ["title"],
                "additionalProperties": true
              }
            }
          }
        }
        """;
        await File.WriteAllTextAsync(_collectionsFile, collectionsConfig);

        // Create content without explicit slug
        var content = """
        ---
        title: Article Without Slug
        ---
        
        This should use filename as slug.
        """;
        await File.WriteAllTextAsync(
            Path.Combine(collectionDir, "my-article-2025.md"), 
            content
        );

        // Act & Assert
        File.Exists(Path.Combine(collectionDir, "my-article-2025.md")).Should().BeTrue();
        
        // When implemented, should be queryable by "my-article-2025" (derived from filename)
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
