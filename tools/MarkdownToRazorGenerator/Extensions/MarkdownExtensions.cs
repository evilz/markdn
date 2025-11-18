using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MarkdownToRazorGenerator.Extensions;

/// <summary>
/// Extension methods for working with Markdown and front matter
/// </summary>
public static class MarkdownExtensions
{
    private static readonly IDeserializer YamlDeserializer = 
        new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithAttemptingUnquotedStringTypeDeserialization()
            .Build();
    
    private static readonly MarkdownPipeline Pipeline 
        = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();

    /// <summary>
    /// Extracts and deserializes YAML front matter from markdown content
    /// </summary>
    /// <typeparam name="T">The type to deserialize the front matter into</typeparam>
    /// <param name="markdown">The markdown content with YAML front matter</param>
    /// <returns>The deserialized front matter object, or default(T) if no front matter exists</returns>
    public static T? GetFrontMatter<T>(this string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        var block = document
            .Descendants<YamlFrontMatterBlock>()
            .FirstOrDefault();

        if (block == null) 
            return default;

        return YamlDeserializer.Deserialize<T>(block.Lines.ToString());
    }

    /// <summary>
    /// Extracts and returns the YAML front matter as a string
    /// </summary>
    /// <param name="markdown">The markdown content with YAML front matter</param>
    /// <returns>The YAML front matter as a string, or null if no front matter exists</returns>
    public static string? GetFrontMatterYaml(this string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        var block = document
            .Descendants<YamlFrontMatterBlock>()
            .FirstOrDefault();

        if (block == null) 
            return null;

        return block.Lines.ToString();
    }

    /// <summary>
    /// Extracts the markdown body (content after front matter)
    /// </summary>
    /// <param name="markdown">The markdown content with YAML front matter</param>
    /// <returns>The markdown body without the front matter</returns>
    public static string GetMarkdownBody(this string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return markdown;

        var document = Markdown.Parse(markdown, Pipeline);
        var block = document
            .Descendants<YamlFrontMatterBlock>()
            .FirstOrDefault();

        if (block == null) 
            return markdown;

        // Get the content after the front matter block
        var lines = markdown.Split('\n');
        
        // Find the end of the YAML block by looking for the second '---'
        int firstDelimiter = -1;
        int secondDelimiter = -1;
        
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed == "---")
            {
                if (firstDelimiter == -1)
                    firstDelimiter = i;
                else
                {
                    secondDelimiter = i;
                    break;
                }
            }
        }

        if (secondDelimiter != -1)
        {
            var bodyLines = lines.Skip(secondDelimiter + 1);
            return string.Join('\n', bodyLines);
        }

        return markdown;
    }
}
