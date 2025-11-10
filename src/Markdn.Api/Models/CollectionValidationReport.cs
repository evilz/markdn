namespace Markdn.Api.Models;

/// <summary>
/// Comprehensive validation report for a collection.
/// </summary>
public class CollectionValidationReport
{
    /// <summary>
    /// Gets or sets the name of the collection.
    /// </summary>
    public required string CollectionName { get; set; }

    /// <summary>
    /// Gets or sets the total number of content items in the collection.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the number of valid items.
    /// </summary>
    public int ValidItems { get; set; }

    /// <summary>
    /// Gets or sets the number of invalid items.
    /// </summary>
    public int InvalidItems { get; set; }

    /// <summary>
    /// Gets or sets the validation timestamp.
    /// </summary>
    public DateTime ValidationTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the list of validation errors for invalid items.
    /// </summary>
    public List<ItemValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Gets whether all items passed validation.
    /// </summary>
    public bool IsValid => InvalidItems == 0;
}

/// <summary>
/// Represents a validation error for a specific content item.
/// </summary>
public class ItemValidationError
{
    /// <summary>
    /// Gets or sets the file path of the item with errors.
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// Gets or sets the slug of the item.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// Gets or sets the list of validation error messages.
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();
}
