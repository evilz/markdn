using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Markdn.Api.Models;
using Markdn.Api.Services;
using Markdn.Api.Validation;

namespace Markdn.Api.Tests.Unit.Validation;

/// <summary>
/// Unit tests for ContentItemValidator.
/// </summary>
public class ContentItemValidatorTests
{
    private readonly Mock<ISchemaValidator> _mockSchemaValidator;
    private readonly Mock<ILogger<ContentItemValidator>> _mockLogger;
    private readonly ContentItemValidator _validator;

    public ContentItemValidatorTests()
    {
        _mockSchemaValidator = new Mock<ISchemaValidator>();
        _mockLogger = new Mock<ILogger<ContentItemValidator>>();
        _validator = new ContentItemValidator(_mockSchemaValidator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ValidateAsync_WithValidContent_ShouldReturnValidResult()
    {
        // Arrange
        var schema = new CollectionSchema
        {
            Properties = new Dictionary<string, FieldDefinition>
            {
                ["title"] = new() { Type = FieldType.String },
                ["author"] = new() { Type = FieldType.String }
            },
            Required = new List<string> { "title" }
        };

        var contentItem = new ContentItem
        {
            Slug = "test-post",
            FilePath = "/content/test-post.md",
            CustomFields = new Dictionary<string, object>
            {
                ["title"] = "Test Post",
                ["author"] = "Test Author"
            }
        };

        // Mock the schema validator to return success
        _mockSchemaValidator
            .Setup(x => x.ValidateAsync(It.IsAny<NJsonSchema.JsonSchema>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult { IsValid = true });

        // Act
        var result = await _validator.ValidateAsync(contentItem, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithMissingRequiredField_ShouldReturnInvalid()
    {
        // Arrange
        var schema = new CollectionSchema
        {
            Name = "blog",
            Fields = new List<SchemaField>
            {
                new() { Name = "title", Type = FieldType.String, Required = true },
                new() { Name = "author", Type = FieldType.String, Required = false }
            }
        };

        var contentItem = new ContentItem
        {
            Slug = "test-post",
            FilePath = "/content/test-post.md",
            CustomFields = new Dictionary<string, object>
            {
                ["author"] = "Test Author"
                // Missing 'title' which is required
            }
        };

        // Mock the schema validator to return validation errors
        _mockSchemaValidator
            .Setup(x => x.ValidateAsync(It.IsAny<NJsonSchema.JsonSchema>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new() { PropertyPath = "title", Message = "Required property 'title' is missing.", Severity = ValidationSeverity.Error }
                }
            });

        // Act
        var result = await _validator.ValidateAsync(contentItem, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].PropertyPath.Should().Be("title");
        result.Errors[0].Severity.Should().Be(ValidationSeverity.Error);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidFieldType_ShouldReturnInvalid()
    {
        // Arrange
        var schema = new CollectionSchema
        {
            Name = "blog",
            Fields = new List<SchemaField>
            {
                new() { Name = "title", Type = FieldType.String, Required = true },
                new() { Name = "publishedAt", Type = FieldType.DateTime, Required = true }
            }
        };

        var contentItem = new ContentItem
        {
            Slug = "test-post",
            FilePath = "/content/test-post.md",
            CustomFields = new Dictionary<string, object>
            {
                ["title"] = "Test Post",
                ["publishedAt"] = "not-a-date" // Invalid date format
            }
        };

        // Mock the schema validator to return type validation errors
        _mockSchemaValidator
            .Setup(x => x.ValidateAsync(It.IsAny<NJsonSchema.JsonSchema>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new() { PropertyPath = "publishedAt", Message = "Invalid date format.", Severity = ValidationSeverity.Error }
                }
            });

        // Act
        var result = await _validator.ValidateAsync(contentItem, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].PropertyPath.Should().Be("publishedAt");
    }

    [Fact]
    public async Task ValidateAsync_WithExtraFields_ShouldReturnWarning()
    {
        // Arrange
        var schema = new CollectionSchema
        {
            Name = "blog",
            Fields = new List<SchemaField>
            {
                new() { Name = "title", Type = FieldType.String, Required = true }
            }
        };

        var contentItem = new ContentItem
        {
            Slug = "test-post",
            FilePath = "/content/test-post.md",
            CustomFields = new Dictionary<string, object>
            {
                ["title"] = "Test Post",
                ["extraField"] = "This field is not in the schema"
            }
        };

        // Mock the schema validator to return a warning for extra fields
        _mockSchemaValidator
            .Setup(x => x.ValidateAsync(It.IsAny<NJsonSchema.JsonSchema>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                IsValid = true,
                Errors = new List<ValidationError>
                {
                    new() { PropertyPath = "extraField", Message = "Property is not defined in the schema.", Severity = ValidationSeverity.Warning }
                }
            });

        // Act
        var result = await _validator.ValidateAsync(contentItem, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue(); // Warnings don't make content invalid
        result.Errors.Should().ContainSingle();
        result.Errors[0].Severity.Should().Be(ValidationSeverity.Warning);
    }
}
