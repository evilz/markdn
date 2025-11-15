using Markdn.Pico.Models;

namespace Markdn.Pico.Services;

public interface IPostsService
{
    List<Post> GetAllPosts();
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
        // Convert generated PostsEntry to Post model
        var entries = _contentService.GetCollection();
        return entries.Select(e => new Post
        {
            Slug = e.Slug,
            Title = e.Title,
            PubDate = e.PubDate,
            Description = e.Description
        }).ToList();
    }
}
