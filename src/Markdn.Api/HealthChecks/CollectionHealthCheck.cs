using Markdn.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Markdn.Api.HealthChecks;

/// <summary>
/// Health check for collection validation status.
/// Reports the health status based on collection loading and validation state.
/// </summary>
public class CollectionHealthCheck : IHealthCheck
{
    private readonly ICollectionLoader _collectionLoader;
    private readonly ILogger<CollectionHealthCheck> _logger;

    public CollectionHealthCheck(
        ICollectionLoader collectionLoader,
        ILogger<CollectionHealthCheck> logger)
    {
        _collectionLoader = collectionLoader;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to load collections to verify configuration is valid
            var collections = await _collectionLoader.LoadCollectionsAsync(cancellationToken);

            if (collections == null || collections.Count == 0)
            {
                _logger.LogWarning("No collections found during health check");
                return HealthCheckResult.Degraded(
                    "No collections configured",
                    data: new Dictionary<string, object>
                    {
                        ["collection_count"] = 0
                    });
            }

            var data = new Dictionary<string, object>
            {
                ["collection_count"] = collections.Count,
                ["collection_names"] = string.Join(", ", collections.Keys)
            };

            _logger.LogDebug("Health check passed with {Count} collections", collections.Count);

            return HealthCheckResult.Healthy(
                $"Collections loaded successfully: {collections.Count} collection(s)",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed: Unable to load collections");
            return HealthCheckResult.Unhealthy(
                "Failed to load collections",
                ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
        }
    }
}
