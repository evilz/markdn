namespace Markdn.Blazor.App.Models;

/// <summary>
/// Represents metadata from markdown frontmatter that can be passed to layouts.
/// This allows layouts to access page-specific information like tags, dates, author, etc.
/// </summary>
public class PageMetadata
{
    /// <summary>
    /// The title of the page
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// The slug/identifier of the page
    /// </summary>
    public string? Slug { get; set; }
    
    /// <summary>
    /// The route of the page
    /// </summary>
    public string? Route { get; set; }
    
    /// <summary>
    /// The summary or description of the page
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// Publication or creation date
    /// </summary>
    public DateTime? Date { get; set; }
    
    /// <summary>
    /// List of tags associated with the page
    /// </summary>
    public List<string>? Tags { get; set; }
    
    /// <summary>
    /// Additional metadata properties from frontmatter
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }
    
    /// <summary>
    /// Gets a value from additional data with type casting
    /// </summary>
    public T? GetValue<T>(string key)
    {
        if (AdditionalData?.TryGetValue(key, out var value) == true && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
}
