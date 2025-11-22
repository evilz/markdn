
using Markdn.Pico.Components;
using Markdn.Pico.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IPostsService, PostsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapGet("/rss.xml", async (IPostsService postsService, HttpContext context) =>
{
    var posts = postsService.GetAllPosts();
    var rss = new System.Text.StringBuilder();
    rss.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    rss.AppendLine("<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">");
    rss.AppendLine("<channel>");
    rss.AppendLine($"<title>Markdn.Pico</title>");
    rss.AppendLine($"<description>Astro styled with Pico CSS</description>");
    rss.AppendLine($"<link>{context.Request.Scheme}://{context.Request.Host}</link>");
    rss.AppendLine($"<atom:link href=\"{context.Request.Scheme}://{context.Request.Host}/rss.xml\" rel=\"self\" type=\"application/rss+xml\" />");
    
    foreach (var post in posts)
    {
        var escapedRoute = System.Security.SecurityElement.Escape(post.Route ?? string.Empty);
        rss.AppendLine("<item>");
        rss.AppendLine($"<title>{System.Security.SecurityElement.Escape(post.Title)}</title>");
        rss.AppendLine($"<description>{System.Security.SecurityElement.Escape(post.Description ?? string.Empty)}</description>");
        rss.AppendLine($"<link>{context.Request.Scheme}://{context.Request.Host}{escapedRoute}</link>");
        rss.AppendLine($"<guid>{context.Request.Scheme}://{context.Request.Host}{escapedRoute}</guid>");
        rss.AppendLine($"<pubDate>{post.PubDate:R}</pubDate>");
        rss.AppendLine("</item>");
    }
    
    rss.AppendLine("</channel>");
    rss.AppendLine("</rss>");
    
    context.Response.ContentType = "application/rss+xml";
    await context.Response.WriteAsync(rss.ToString());
});

app.MapGet("/sitemap.xml", async (IPostsService postsService, HttpContext context) =>
{
    var posts = postsService.GetAllPosts();
    var sitemap = new System.Text.StringBuilder();
    sitemap.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sitemap.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
    
    // Home
    sitemap.AppendLine("<url>");
    sitemap.AppendLine($"<loc>{context.Request.Scheme}://{context.Request.Host}/</loc>");
    sitemap.AppendLine("<changefreq>daily</changefreq>");
    sitemap.AppendLine("<priority>1.0</priority>");
    sitemap.AppendLine("</url>");
    
    foreach (var post in posts)
    {
        var escapedRoute = System.Security.SecurityElement.Escape(post.Route ?? string.Empty);
        sitemap.AppendLine("<url>");
        sitemap.AppendLine($"<loc>{context.Request.Scheme}://{context.Request.Host}{escapedRoute}</loc>");
        sitemap.AppendLine($"<lastmod>{post.PubDate:yyyy-MM-dd}</lastmod>");
        sitemap.AppendLine("<changefreq>weekly</changefreq>");
        sitemap.AppendLine("<priority>0.8</priority>");
        sitemap.AppendLine("</url>");
    }
    
    sitemap.AppendLine("</urlset>");
    
    context.Response.ContentType = "application/xml";
    await context.Response.WriteAsync(sitemap.ToString());
});
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
