using Markdn.Api.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Markdn.Api.Services;

/// <summary>
/// Service for parsing YAML front-matter from Markdown content
/// </summary>
public class FrontMatterParser
{
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the FrontMatterParser with YAML deserializer configuration
    /// </summary>
    public FrontMatterParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Parses YAML front-matter from Markdown content
    /// </summary>
    /// <param name="content">Full Markdown content including front-matter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed front-matter metadata</returns>
    public Task<FrontMatter> ParseAsync(string content, CancellationToken cancellationToken)
    {
        var frontMatter = new FrontMatter();

        // Extract front-matter between --- delimiters
        var lines = content.Split('\n');
        if (lines.Length < 3 || !lines[0].Trim().Equals("---"))
        {
            return Task.FromResult(frontMatter);
        }

        var endIndex = -1;
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim().Equals("---"))
            {
                endIndex = i;
                break;
            }
        }

        if (endIndex == -1)
        {
            return Task.FromResult(frontMatter);
        }

        var yamlContent = string.Join('\n', lines[1..endIndex]);

        try
        {
            var data = _deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
            
            if (data == null)
            {
                return Task.FromResult(frontMatter);
            }

            // Map standard fields
            if (data.TryGetValue("title", out var title))
                frontMatter.Title = title?.ToString();
            
            if (data.TryGetValue("date", out var date))
                frontMatter.Date = date?.ToString();
            
            if (data.TryGetValue("author", out var author))
                frontMatter.Author = author?.ToString();
            
            if (data.TryGetValue("category", out var category))
                frontMatter.Category = category?.ToString();
            
            if (data.TryGetValue("description", out var description))
                frontMatter.Description = description?.ToString();
            
            if (data.TryGetValue("slug", out var slug))
                frontMatter.Slug = slug?.ToString();

            if (data.TryGetValue("tags", out var tagsObj) && tagsObj is List<object> tagsList)
            {
                frontMatter.Tags = tagsList.Select(t => t?.ToString() ?? string.Empty)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
            }

            // Store all additional properties
            foreach (var kvp in data)
            {
                frontMatter.AdditionalProperties[kvp.Key] = kvp.Value;
            }
        }
        catch
        {
            // Invalid YAML - return empty front-matter
            return Task.FromResult(frontMatter);
        }

        return Task.FromResult(frontMatter);
    }
}
