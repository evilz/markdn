using Markdn.Api.Models;

namespace Markdn.Api.Services;

/// <summary>
/// Service for loading collection configurations and schemas.
/// </summary>
public interface ICollectionLoader
{
    /// <summary>
    /// Loads all collections from the configuration file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of collections keyed by collection name.</returns>
    Task<Dictionary<string, Collection>> LoadCollectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a specific collection by name.
    /// </summary>
    /// <param name="name">The collection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection or null if not found.</returns>
    Task<Collection?> LoadCollectionAsync(string name, CancellationToken cancellationToken = default);
}
