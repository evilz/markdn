using Microsoft.AspNetCore.Components;
using Markdn.Pico.Models;

namespace Markdn.Pico.Services;

public interface IPostsService
{
    List<Post> GetAllPosts();
    Post? GetPostBySlug(string slug);
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
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return _contentService.GetEntry(slug);
    }

    public RenderFragment? GetPostComponent(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return _contentService.GetComponent(slug);
    }
}
