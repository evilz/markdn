using Microsoft.AspNetCore.Components;
using Markdn.Pico.Models;

namespace Markdn.Pico.Services;

public interface IPostsService
{
    /// <summary>
    /// Returns a list of all available posts.
    /// </summary>
    List<Post> GetAllPosts();

    /// <summary>
    /// Retrieves a post by its slug identifier.
    /// </summary>
    /// <param name="slug">The unique slug of the post.</param>
    /// <returns>
    /// The <see cref="Post"/> object if found; otherwise, <c>null</c>.
    /// </returns>
    Post? GetPostBySlug(string slug);

    /// <summary>
    /// Gets a <see cref="RenderFragment"/> representing the post content for the specified slug.
    /// </summary>
    /// <param name="slug">The unique slug of the post.</param>
    /// <returns>
    /// A <see cref="RenderFragment"/> for rendering the post if found; otherwise, <c>null</c>.
    /// </returns>
    RenderFragment? GetPostComponent(string slug);
}

public class PostsService : IPostsService
{
    private readonly Content.IPostsService _contentService;

    public PostsService()
    {
        // Use the source-generated content service
        _contentService = new Content.PostsService();
    }

    public List<Post> GetAllPosts()
    {
        return _contentService.GetCollection();
    }

    public Post? GetPostBySlug(string slug)
    {
        if (!IsValidSlug(slug))
        {
            return null;
        }

        return _contentService.GetEntry(slug);
    }

    public RenderFragment? GetPostComponent(string slug)
    {
        if (!IsValidSlug(slug))
        {
            return null;
        }

        return _contentService.GetComponent(slug);
    }

    private static bool IsValidSlug(string slug)
    {
        return !string.IsNullOrWhiteSpace(slug);
    }
}
