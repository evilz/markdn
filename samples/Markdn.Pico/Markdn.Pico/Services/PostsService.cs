using Markdn.Pico.Models;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Markdn.Pico.Services;

public interface IPostsService
{
    List<Post> GetAllPosts();
    Post? GetPostBySlug(string slug);
}

public class PostsService : IPostsService
{
    private static readonly List<Post> EmptyPostsList = new();
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PostsService> _logger;
    private List<Post>? _cachedPosts;
    private readonly object _cacheLock = new object();

    public PostsService(IWebHostEnvironment environment, ILogger<PostsService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public List<Post> GetAllPosts()
    {
        if (_cachedPosts != null)
        {
            return _cachedPosts;
        }

        lock (_cacheLock)
        {
            // Double-check pattern for thread safety
            if (_cachedPosts != null)
            {
                return _cachedPosts;
            }

            var contentPath = Path.Combine(_environment.ContentRootPath, "Content", "Posts");
            if (!Directory.Exists(contentPath))
            {
                return EmptyPostsList;
            }

            var posts = new List<Post>();
            var pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .Build();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            foreach (var file in Directory.GetFiles(contentPath, "*.md", SearchOption.AllDirectories))
            {
                try
                {
                    var markdown = File.ReadAllText(file);
                    var document = Markdown.Parse(markdown, pipeline);
                    var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

                    if (yamlBlock != null)
                    {
                        // Extract YAML content between the start and end of the block
                        var yaml = markdown[yamlBlock.Span.Start..(yamlBlock.Span.Start + yamlBlock.Span.Length)];
                        
                        // Remove only the leading and trailing --- markers
                        var lines = yaml.Split('\n', StringSplitOptions.None);
                        if (lines.Length > 2 && lines[0].Trim() == "---" && lines[^1].Trim() == "---")
                        {
                            // Skip first and last lines if they are markers
                            var yamlContent = string.Join('\n', lines[1..^1]);
                            yaml = yamlContent.Trim();
                        }
                        
                        var post = deserializer.Deserialize<Post>(yaml);
                        
                        // Set slug from filename if not in yaml
                        if (string.IsNullOrEmpty(post.Slug))
                        {
                            post.Slug = Path.GetFileNameWithoutExtension(file);
                        }

                        // Set route if not present
                        if (string.IsNullOrEmpty(post.Route))
                        {
                            var directory = Path.GetDirectoryName(file);
                            var dirName = Path.GetFileName(directory)?.ToLowerInvariant();
                            
                            if (dirName == "pages")
                            {
                                post.Route = $"/{post.Slug}";
                            }
                            else if (dirName == "projects")
                            {
                                post.Route = $"/projects/{post.Slug}";
                            }
                            else
                            {
                                post.Route = $"/posts/{post.Slug}";
                            }
                        }

                        posts.Add(post);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing {File}", file);
                }
            }

            _cachedPosts = posts.OrderByDescending(p => p.PubDate).ToList();
            return _cachedPosts;
        }
    }

    public Post? GetPostBySlug(string slug)
    {
        return GetAllPosts().FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }
}
