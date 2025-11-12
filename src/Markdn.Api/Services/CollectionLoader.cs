using System.Text.Json;
using Markdn.Api.Configuration;
using Markdn.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Markdn.Api.Services;

/// <summary>
/// Service for loading collection configurations and schemas from collections.json.
/// Monitors the configuration file for changes and notifies subscribers.
/// </summary>
public class CollectionLoader : ICollectionLoader, IDisposable
{
    private readonly CollectionsOptions _options;
    private readonly MarkdnOptions _markdnOptions;
    private readonly ILogger<CollectionLoader> _logger;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    private FileSystemWatcher? _configWatcher;
    private Timer? _debounceTimer;
    private const int DebounceDelayMs = 300;
    private const string CacheKey = "all_collections";

    /// <summary>
    /// Event raised when the collections configuration file changes.
    /// </summary>
    public event EventHandler<string>? ConfigurationChanged;

    public CollectionLoader(
        IOptions<CollectionsOptions> options,
        IOptions<MarkdnOptions> markdnOptions,
        IMemoryCache cache,
        ILogger<CollectionLoader> logger)
    {
        _options = options.Value;
        _markdnOptions = markdnOptions.Value;
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        InitializeFileWatcher();
    }

    /// <summary>
    /// Backwards-compatible constructor for callers that don't provide MarkdnOptions.
    /// Delegates to the main constructor using default MarkdnOptions.
    /// </summary>
    public CollectionLoader(
        IOptions<CollectionsOptions> options,
        IMemoryCache cache,
        ILogger<CollectionLoader> logger)
        : this(options, Microsoft.Extensions.Options.Options.Create(new MarkdnOptions()), cache, logger)
    {
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, Collection>> LoadCollectionsAsync(
        CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cache.TryGetValue<Dictionary<string, Collection>>(CacheKey, out var cachedCollections) 
            && cachedCollections != null)
        {
            _logger.LogDebug("Returning {Count} cached collections", cachedCollections.Count);
            return cachedCollections;
        }

        var resolvedConfigPath = GetResolvedConfigurationFilePath();
        _logger.LogInformation("Loading collections from {ConfigFile}", resolvedConfigPath);

        if (!File.Exists(resolvedConfigPath))
        {
            _logger.LogWarning("Collections configuration file not found at {ConfigFile}", 
                resolvedConfigPath);
            throw new FileNotFoundException(
                $"Collections configuration file not found: {resolvedConfigPath}",
                resolvedConfigPath);
        }

        try
        {
            using var stream = File.OpenRead(resolvedConfigPath);
            var config = await JsonSerializer.DeserializeAsync<CollectionsConfiguration>(
                stream, _jsonOptions, cancellationToken).ConfigureAwait(false);

            if (config == null || config.Collections == null)
            {
                _logger.LogWarning("Collections configuration is null or empty");
                return new Dictionary<string, Collection>();
            }

            var collections = new Dictionary<string, Collection>();

            foreach (var (name, definition) in config.Collections)
            {
                try
                {
                    var collection = ParseCollection(name, definition, config.ContentRootPath);
                    collections[name] = collection;
                    _logger.LogDebug("Loaded collection {CollectionName} with {PropertyCount} schema properties",
                        name, collection.Schema.Properties.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse collection {CollectionName}", name);
                    throw;
                }
            }

            _logger.LogInformation("Successfully loaded {CollectionCount} collections", collections.Count);

            // Cache for 10 minutes (will be invalidated on file change)
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                Size = 1 // Each collection definition counts as 1 unit
            };

            // Defensive: some test harnesses provide a mocked IMemoryCache where CreateEntry
            // may return null (causing Set extension to throw NullReferenceException).
            // Wrap cache set in try/catch and continue if caching fails — caching is an
            // optimization and should not break collection loading.
            try
            {
                _cache?.Set(CacheKey, collections, cacheOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache collections, continuing without cache");
            }

            return collections;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse collections configuration file");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading collections");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Collection?> LoadCollectionAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var collections = await LoadCollectionsAsync(cancellationToken).ConfigureAwait(false);
        return collections.TryGetValue(name, out var collection) ? collection : null;
    }

    private Collection ParseCollection(string name, CollectionDefinition definition, string _)
    {
        // Parse the schema from the dynamic object
        var schemaJson = JsonSerializer.Serialize(definition.Schema);
        var schemaElement = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        var schema = ParseSchema(schemaElement);

        return new Collection
        {
            Name = name,
            FolderPath = definition.Folder,
            Schema = schema
        };
    }

    private CollectionSchema ParseSchema(JsonElement schemaElement)
    {
        var schema = new CollectionSchema
        {
            Properties = new Dictionary<string, FieldDefinition>()
        };

        if (schemaElement.TryGetProperty("type", out var typeElement))
        {
            schema.Type = typeElement.GetString() ?? "object";
        }

        if (schemaElement.TryGetProperty("title", out var titleElement))
        {
            schema.Title = titleElement.GetString();
        }

        if (schemaElement.TryGetProperty("description", out var descElement))
        {
            schema.Description = descElement.GetString();
        }

        if (schemaElement.TryGetProperty("additionalProperties", out var additionalPropsElement))
        {
            schema.AdditionalProperties = additionalPropsElement.GetBoolean();
        }

        if (schemaElement.TryGetProperty("required", out var requiredElement) && 
            requiredElement.ValueKind == JsonValueKind.Array)
        {
            schema.Required = requiredElement.EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (schemaElement.TryGetProperty("properties", out var propertiesElement) &&
            propertiesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in propertiesElement.EnumerateObject())
            {
                var fieldDef = ParseFieldDefinition(property.Name, property.Value);
                schema.Properties[property.Name] = fieldDef;
            }
        }

        return schema;
    }

    private FieldDefinition ParseFieldDefinition(string name, JsonElement fieldElement)
    {
        var field = new FieldDefinition
        {
            Name = name,
            Type = FieldType.String // Default
        };

        if (fieldElement.TryGetProperty("type", out var typeElement))
        {
            var typeString = typeElement.GetString();
            field.Type = typeString?.ToLowerInvariant() switch
            {
                "string" => FieldType.String,
                "number" => FieldType.Number,
                "integer" => FieldType.Number,
                "boolean" => FieldType.Boolean,
                "array" => FieldType.Array,
                "object" => FieldType.String, // Treat objects as strings for now
                _ => FieldType.String
            };
        }

        if (fieldElement.TryGetProperty("format", out var formatElement))
        {
            field.Format = formatElement.GetString();
            if (field.Format == "date" || field.Format == "date-time")
            {
                field.Type = FieldType.Date;
            }
        }

        if (fieldElement.TryGetProperty("pattern", out var patternElement))
        {
            field.Pattern = patternElement.GetString();
        }

        if (fieldElement.TryGetProperty("minLength", out var minLengthElement))
        {
            field.MinLength = minLengthElement.GetInt32();
        }

        if (fieldElement.TryGetProperty("maxLength", out var maxLengthElement))
        {
            field.MaxLength = maxLengthElement.GetInt32();
        }

        if (fieldElement.TryGetProperty("minimum", out var minimumElement))
        {
            if (minimumElement.TryGetDecimal(out var minValue))
            {
                field.Minimum = minValue;
            }
        }

        if (fieldElement.TryGetProperty("maximum", out var maximumElement))
        {
            if (maximumElement.TryGetDecimal(out var maxValue))
            {
                field.Maximum = maxValue;
            }
        }

        if (fieldElement.TryGetProperty("enum", out var enumElement) &&
            enumElement.ValueKind == JsonValueKind.Array)
        {
            field.Enum = enumElement.EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (fieldElement.TryGetProperty("description", out var descElement))
        {
            field.Description = descElement.GetString();
        }

        if (field.Type == FieldType.Array && fieldElement.TryGetProperty("items", out var itemsElement))
        {
            field.Items = ParseFieldDefinition($"{name}_item", itemsElement);
        }

        return field;
    }

    private void InitializeFileWatcher()
    {
        var resolvedConfigPath = GetResolvedConfigurationFilePath();

        if (!File.Exists(resolvedConfigPath))
        {
            _logger.LogWarning("Cannot watch non-existent configuration file: {ConfigFile}",
                resolvedConfigPath);
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(resolvedConfigPath);
            var fileName = Path.GetFileName(resolvedConfigPath);

            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
            {
                _logger.LogWarning("Invalid configuration file path: {ConfigFile}", 
                    _options.ConfigurationFilePath);
                return;
            }

            _configWatcher = new FileSystemWatcher(directory)
            {
                Filter = fileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            _configWatcher.Changed += OnConfigFileChanged;
            _configWatcher.Created += OnConfigFileChanged;
            _configWatcher.Renamed += OnConfigFileChanged;

            _logger.LogInformation("Started watching collections configuration file: {ConfigFile}",
                resolvedConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize file watcher for collections configuration");
        }
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // Dispose existing timer if it exists
        _debounceTimer?.Dispose();

        // Create new debounce timer
        _debounceTimer = new Timer(_ =>
        {
            try
            {
                _logger.LogInformation("Collections configuration file changed, invalidating cache");

                // Invalidate cache
                _cache.Remove(CacheKey);

                // Invalidate all collection item caches
                // Note: This is a simple approach. A more sophisticated approach would track
                // which collections changed and only invalidate those.
                _logger.LogInformation("Cache invalidated, collections will reload on next access");

                // Raise event for subscribers
                ConfigurationChanged?.Invoke(this, GetResolvedConfigurationFilePath());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling configuration file change");
            }
            finally
            {
                _debounceTimer?.Dispose();
                _debounceTimer = null;
            }
        }, null, DebounceDelayMs, Timeout.Infinite);
    }

    private string GetResolvedConfigurationFilePath()
    {
        var configPath = _options.ConfigurationFilePath ?? "content/collections.json";

        if (Path.IsPathRooted(configPath))
        {
            return Path.GetFullPath(configPath);
        }

        var contentDir = _markdnOptions?.ContentDirectory ?? "content";

        // If the configPath already begins with the content directory segment (for example
        // configPath == "content/collections.json" and contentDir == "content"), combining
        // them would produce "content/content/collections.json" which is incorrect. Detect
        // that case and return the full path for configPath directly. Otherwise combine
        // contentDir and configPath as expected.
        var splitSeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var configFirstSegment = configPath.Split(splitSeparators, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(configFirstSegment) &&
            string.Equals(configFirstSegment, contentDir, StringComparison.OrdinalIgnoreCase))
        {
            // configPath already targets the content directory; return as-is (resolved)
            return Path.GetFullPath(configPath);
        }

        var combined = Path.Combine(contentDir, configPath);
        return Path.GetFullPath(combined);
    }

    public void Dispose()
    {
        if (_configWatcher != null)
        {
            _configWatcher.Changed -= OnConfigFileChanged;
            _configWatcher.Created -= OnConfigFileChanged;
            _configWatcher.Renamed -= OnConfigFileChanged;
            _configWatcher.Dispose();
            _configWatcher = null;
        }

        _debounceTimer?.Dispose();
        _debounceTimer = null;
    }
}
