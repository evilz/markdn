using System.Reflection;
using Markdn.Pico.Models;

namespace Markdn.Pico.Services;

public interface IPostsService
{
    List<Post> GetAllPosts();
}

public class PostsService : IPostsService
{
    private const string BlogNamespace = "Markdn.Pico"; // TODO: change this
    public List<Post> GetAllPosts()
    {
        try
        {
            var assembly = typeof(Program).Assembly;
            var componentBaseType = typeof(Microsoft.AspNetCore.Components.ComponentBase);

            // Use DefinedTypes for better performance (avoids allocating Type array)
            // Filter first, then materialize to reduce allocations
            var blogPosts = assembly.DefinedTypes
                .Where(t => t.Namespace?.StartsWith(BlogNamespace, StringComparison.Ordinal) == true
                            && t.IsClass
                            && !t.IsAbstract
                            && componentBaseType.IsAssignableFrom(t))
                .Select(type =>
                {
                    // Use generic GetCustomAttribute<T>() for better performance
                    var routeAttr = type.GetCustomAttribute<Microsoft.AspNetCore.Components.RouteAttribute>();
                    if (routeAttr == null)
                    {
                        return null;
                    }

                    return new Post
                    {
                        Title = type.Name,
                        Slug = routeAttr.Template,
                        PubDate = DateTime.UtcNow
                    };
                })
                .Where(p => p != null)
                .Cast<Post>()
                .ToList();

            return blogPosts;
        }
        catch (Exception)
        {
            // Return empty list if we can't load blog posts
            return [];
        }
    }
}
