using System.Text.Json;
using NJsonSchema;
using NJsonSchema.Validation;
using Markdn.Api.Models;
using NjsValidationError = NJsonSchema.Validation.ValidationError;

namespace Markdn.Api.Services;

/// <summary>
/// Service for validating content against JSON Schema definitions using NJsonSchema.
/// </summary>
public class SchemaValidator : ISchemaValidator
{
    private readonly ILogger<SchemaValidator> _logger;
    private readonly TimeSpan _validationTimeout;

    public SchemaValidator(ILogger<SchemaValidator> logger, IConfiguration configuration)
    {
        _logger = logger;
        _validationTimeout = TimeSpan.FromSeconds(
            configuration.GetValue<int>("Collections:ValidationTimeoutSeconds", 30)
        );
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(
        JsonSchema schema,
        object content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting validation against schema: {SchemaTitle}", schema.Title ?? "Untitled");

            // Create timeout CancellationTokenSource
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_validationTimeout);

            // Serialize content to JSON for validation
            var contentJson = JsonSerializer.Serialize(content);

            // Validate with timeout
            var validationTask = Task.Run(() =>
            {
                try
                {
                    return schema.Validate(contentJson);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Schema validation threw an exception");
                    throw;
                }
            }, timeoutCts.Token);

            ICollection<NjsValidationError> validationErrors;
            try
            {
                validationErrors = await validationTask;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("Validation timed out after {Timeout} seconds", _validationTimeout.TotalSeconds);
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<Models.ValidationError>
                    {
                        new()
                        {
                            FieldName = "",
                            ErrorType = ValidationErrorType.Other,
                            Message = $"Validation timed out after {_validationTimeout.TotalSeconds} seconds",
                            ActualValue = null
                        }
                    },
                    Warnings = new List<ValidationWarning>(),
                    ValidatedAt = DateTime.UtcNow
                };
            }

            // Convert NJsonSchema ValidationErrors to our ValidationError model
            var errors = new List<Models.ValidationError>();
            var warnings = new List<ValidationWarning>();

            foreach (var error in validationErrors)
            {
                var fieldName = error.Path?.TrimStart('#', '/').Replace('/', '.') ?? "";
                
                // Check if this is a warning (extra property)
                if (error.Kind == ValidationErrorKind.AdditionalPropertiesNotValid)
                {
                    warnings.Add(new ValidationWarning
                    {
                        FieldName = fieldName,
                        WarningType = ValidationWarningType.ExtraField,
                        Message = $"Extra field '{fieldName}' is present but not defined in schema"
                    });
                    continue;
                }

                var errorType = MapValidationErrorKind(error.Kind);
                var expectedType = error.Schema?.Type != null 
                    ? string.Join("|", error.Schema.Type.ToString().Split(',').Select(t => t.Trim()))
                    : null;

                errors.Add(new Models.ValidationError
                {
                    FieldName = fieldName,
                    ErrorType = errorType,
                    Message = error.ToString(),
                    ExpectedType = expectedType,
                    ActualValue = null // NJsonSchema doesn't provide the actual value easily
                });
            }

            var isValid = errors.Count == 0;
            var result = new ValidationResult
            {
                IsValid = isValid,
                Errors = errors,
                Warnings = warnings,
                ValidatedAt = DateTime.UtcNow
            };

            if (isValid)
            {
                _logger.LogInformation("Validation passed with {WarningCount} warning(s)", warnings.Count);
            }
            else
            {
                _logger.LogWarning("Validation failed with {ErrorCount} error(s) and {WarningCount} warning(s)", 
                    errors.Count, warnings.Count);
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON schema definition");
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<Models.ValidationError>
                {
                    new()
                    {
                        FieldName = "",
                        ErrorType = ValidationErrorType.InvalidSchema,
                        Message = $"Invalid JSON schema definition: {ex.Message}",
                        ActualValue = null
                    }
                },
                Warnings = new List<ValidationWarning>(),
                ValidatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error during schema validation");
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<Models.ValidationError>
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

    private static ValidationErrorType MapValidationErrorKind(ValidationErrorKind kind)
    {
        return kind switch
        {
            ValidationErrorKind.NoTypeValidates => ValidationErrorType.MissingRequiredField,
            ValidationErrorKind.StringExpected or
            ValidationErrorKind.NumberExpected or
            ValidationErrorKind.BooleanExpected or
            ValidationErrorKind.ArrayExpected or
            ValidationErrorKind.ObjectExpected => ValidationErrorType.TypeMismatch,
            ValidationErrorKind.PatternMismatch => ValidationErrorType.PatternMismatch,
            ValidationErrorKind.NumberTooBig => ValidationErrorType.MaximumExceeded,
            ValidationErrorKind.NumberTooSmall => ValidationErrorType.MinimumViolation,
            ValidationErrorKind.StringTooLong => ValidationErrorType.MaxLengthExceeded,
            ValidationErrorKind.StringTooShort => ValidationErrorType.MinLengthViolation,
            ValidationErrorKind.TooManyItems or
            ValidationErrorKind.TooFewItems => ValidationErrorType.Other,
            ValidationErrorKind.NotInEnumeration => ValidationErrorType.EnumViolation,
            ValidationErrorKind.DateTimeExpected or
            ValidationErrorKind.EmailExpected or
            ValidationErrorKind.UriExpected or
            ValidationErrorKind.IpV4Expected or
            ValidationErrorKind.IpV6Expected => ValidationErrorType.Other,
            _ => ValidationErrorType.Other
        };
    }
}
