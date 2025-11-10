using NJsonSchema;
using Markdn.Api.Models;

namespace Markdn.Api.Services;

/// <summary>
/// Service for validating content against JSON Schema definitions.
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Validates content against a JSON Schema.
    /// </summary>
    /// <param name="schema">The JSON Schema to validate against.</param>
    /// <param name="content">The content to validate (typically a Dictionary or JObject).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A validation result containing errors and warnings.</returns>
    Task<ValidationResult> ValidateAsync(JsonSchema schema, object content, CancellationToken cancellationToken = default);
}
