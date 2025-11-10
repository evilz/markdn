using Markdig;
using Markdig.Extensions.AutoIdentifiers;

namespace Markdn.SourceGenerators.Parsers;

/// <summary>
/// Builds and configures Markdig pipeline for Markdown parsing.
/// </summary>
internal static class MarkdigPipelineBuilder
{
    /// <summary>
    /// Creates a Markdig pipeline with CommonMark and GitHub Flavored Markdown extensions.
    /// </summary>
    public static MarkdownPipeline Build()
    {
        return new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // Includes tables, task lists, definition lists, etc.
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub) // Generate IDs for headings
            .UseEmojiAndSmiley() // Support emoji :smile:
            .UsePipeTables() // Support pipe tables
            .UseTaskLists() // Support [ ] and [x] checkboxes
            .Build();
    }

    /// <summary>
    /// Creates a minimal pipeline without extensions (for testing).
    /// </summary>
    public static MarkdownPipeline BuildMinimal()
    {
        return new MarkdownPipelineBuilder()
            .Build();
    }
}
