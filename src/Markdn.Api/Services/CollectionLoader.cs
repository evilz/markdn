using System.Text.Json;
using Markdn.Api.Configuration;
using Markdn.Api.Models;
using Microsoft.Extensions.Options;

namespace Markdn.Api.Services;

/// <summary>
/// Service for loading collection configurations and schemas from collections.json.
/// </summary>
public class CollectionLoader : ICollectionLoader
{
    private readonly CollectionsOptions _options;
    private readonly ILogger<CollectionLoader> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CollectionLoader(
        IOptions<CollectionsOptions> options,
        ILogger<CollectionLoader> logger)
    {
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, Collection>> LoadCollectionsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading collections from {ConfigFile}", _options.ConfigurationFilePath);

        if (!File.Exists(_options.ConfigurationFilePath))
        {
            _logger.LogWarning("Collections configuration file not found at {ConfigFile}", 
                _options.ConfigurationFilePath);
            throw new FileNotFoundException(
                $"Collections configuration file not found: {_options.ConfigurationFilePath}",
                _options.ConfigurationFilePath);
        }

        try
        {
            using var stream = File.OpenRead(_options.ConfigurationFilePath);
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

    private Collection ParseCollection(string name, CollectionDefinition definition, string contentRootPath)
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
}
