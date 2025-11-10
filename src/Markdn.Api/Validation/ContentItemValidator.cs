using System.Text.Json;
using Markdn.Api.Models;

namespace Markdn.Api.Validation;

/// <summary>
/// Service for validating ContentItem instances against CollectionSchema definitions.
/// </summary>
public class ContentItemValidator
{
    private readonly Services.ISchemaValidator _schemaValidator;
    private readonly ILogger<ContentItemValidator> _logger;

    public ContentItemValidator(
        Services.ISchemaValidator schemaValidator,
        ILogger<ContentItemValidator> logger)
    {
        _schemaValidator = schemaValidator;
        _logger = logger;
    }

    /// <summary>
    /// Validates a ContentItem against a CollectionSchema.
    /// </summary>
    /// <param name="item">The content item to validate.</param>
    /// <param name="schema">The collection schema to validate against.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A validation result containing errors and warnings.</returns>
    public async Task<ValidationResult> ValidateAsync(
        ContentItem item,
        CollectionSchema schema,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating content item: {Slug} against schema: {SchemaTitle}", 
                item.Slug, schema.Title ?? "Untitled");

            // Convert CollectionSchema to NJsonSchema.JsonSchema
            var jsonSchema = await ConvertToJsonSchemaAsync(schema, cancellationToken);

            // Prepare content for validation (use CustomFields which contains front-matter)
            var contentToValidate = item.CustomFields ?? new Dictionary<string, object>();

            // Validate using SchemaValidator
            var result = await _schemaValidator.ValidateAsync(jsonSchema, contentToValidate, cancellationToken);

            _logger.LogInformation("Content item validation completed: IsValid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}",
                result.IsValid, result.Errors.Count, result.Warnings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating content item: {Slug}", item.Slug);
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new()
                    {
                        FieldName = "",
                        ErrorType = ValidationErrorType.Other,
                        Message = $"Validation error: {ex.Message}",
                        ActualValue = null
                    }
                },
                Warnings = new List<ValidationWarning>(),
                ValidatedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<NJsonSchema.JsonSchema> ConvertToJsonSchemaAsync(
        CollectionSchema schema,
        CancellationToken cancellationToken)
    {
        // Build JSON Schema object from CollectionSchema
        var jsonSchemaObj = new
        {
            type = "object",
            title = schema.Title,
            description = schema.Description,
            properties = schema.Properties.ToDictionary(
                p => p.Key,
                p => ConvertFieldDefinitionToSchemaProperty(p.Value)
            ),
            required = schema.Required,
            additionalProperties = schema.AdditionalProperties
        };

        // Serialize to JSON and parse with NJsonSchema
        var jsonString = JsonSerializer.Serialize(jsonSchemaObj);
        return await NJsonSchema.JsonSchema.FromJsonAsync(jsonString, cancellationToken);
    }

    private static object ConvertFieldDefinitionToSchemaProperty(FieldDefinition field)
    {
        var property = new Dictionary<string, object?>
        {
            ["type"] = field.Type.ToString().ToLowerInvariant()
        };

        if (field.Description != null)
        {
            property["description"] = field.Description;
        }

        if (field.Format != null)
        {
            property["format"] = field.Format;
        }

        if (field.Pattern != null)
        {
            property["pattern"] = field.Pattern;
        }

        if (field.MinLength.HasValue)
        {
            property["minLength"] = field.MinLength.Value;
        }

        if (field.MaxLength.HasValue)
        {
            property["maxLength"] = field.MaxLength.Value;
        }

        if (field.Minimum.HasValue)
        {
            property["minimum"] = field.Minimum.Value;
        }

        if (field.Maximum.HasValue)
        {
            property["maximum"] = field.Maximum.Value;
        }

        if (field.Enum != null && field.Enum.Count > 0)
        {
            property["enum"] = field.Enum;
        }

        if (field.Type == FieldType.Array && field.Items != null)
        {
            property["items"] = ConvertFieldDefinitionToSchemaProperty(field.Items);
        }

        return property;
    }
}
