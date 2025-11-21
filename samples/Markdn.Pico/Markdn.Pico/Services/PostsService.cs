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
    private readonly IWebHostEnvironment _environment;
    private List<Post>? _cachedPosts;

    public PostsService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public List<Post> GetAllPosts()
    {
        if (_cachedPosts != null)
        {
            return _cachedPosts;
        }

        var contentPath = Path.Combine(_environment.ContentRootPath, "Content", "Posts");
        if (!Directory.Exists(contentPath))
        {
            return new List<Post>();
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
                    var yaml = markdown.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);
                    // Remove --- markers
                    yaml = yaml.Replace("---", "").Trim();
                    
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
                Console.WriteLine($"Error parsing {file}: {ex.Message}");
            }
        }

        _cachedPosts = posts.OrderByDescending(p => p.PubDate).ToList();
        return _cachedPosts;
    }

    public Post? GetPostBySlug(string slug)
    {
        return GetAllPosts().FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }
}
