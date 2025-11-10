# Data Model: Content Collections

**Feature**: Content Collections  
**Date**: 2025-11-09  
**Purpose**: Define entities, relationships, and validation rules

## Core Entities

### Collection

Represents a group of related content files with a defined schema.

**Properties**:
- `Name` (string, required): Unique identifier for the collection (e.g., "blog", "docs")
- `FolderPath` (string, required): Relative path to collection folder from content root
- `Schema` (CollectionSchema, required): JSON Schema definition for content validation
- `Items` (IReadOnlyList<ContentItem>): Validated content items in the collection (computed)

**Validation Rules**:
- Name must be lowercase alphanumeric with hyphens (regex: `^[a-z0-9-]+$`)
- Name must be unique across all collections
- FolderPath must exist and be readable
- Schema must be valid JSON Schema (Draft 7 or later)

**Relationships**:
- Has one `CollectionSchema`
- Has many `ContentItem` instances

---

### CollectionSchema

Defines the structure and validation rules for content in a collection.

**Properties**:
- `Type` (string, required): Always "object" for content schemas
- `Properties` (Dictionary<string, FieldDefinition>, required): Field definitions
- `Required` (List<string>): Names of required fields
- `AdditionalProperties` (bool): Whether extra fields are allowed (default: true per spec)
- `Title` (string, optional): Human-readable schema name
- `Description` (string, optional): Schema documentation

**Validation Rules**:
- Must have at least one property defined
- Required field names must exist in Properties dictionary
- Field names must be valid JSON property names (no special characters except underscore)

**Relationships**:
- Belongs to one `Collection`
- Has many `FieldDefinition` instances

---

### FieldDefinition

Defines a single field in a collection schema.

**Properties**:
- `Name` (string, required): Field name (JSON property key)
- `Type` (FieldType enum, required): Data type (String, Number, Boolean, Date, Array)
- `Format` (string, optional): Additional format constraint (e.g., "date", "email", "uri")
- `Pattern` (string, optional): Regex pattern for string validation
- `MinLength` (int?, optional): Minimum string length
- `MaxLength` (int?, optional): Maximum string length
- `Minimum` (decimal?, optional): Minimum numeric value
- `Maximum` (decimal?, optional): Maximum numeric value
- `Enum` (List<string>, optional): Allowed values for enumerated types
- `Items` (FieldDefinition, optional): Schema for array items (when Type = Array)
- `Description` (string, optional): Field documentation

**FieldType Enum**:
```csharp
public enum FieldType
{
    String,
    Number,
    Boolean,
    Date,
    Array
}
```

**Validation Rules**:
- Type must be a valid FieldType value
- If Type is Array, Items must be defined
- Pattern must be valid regex if specified
- MinLength/MaxLength only apply to String type
- Minimum/Maximum only apply to Number type
- Format "date" implies Type should be String
- Enum values must match the field Type

**Relationships**:
- Belongs to one `CollectionSchema`
- If Type = Array, contains one nested `FieldDefinition` for items

---

### ContentItem

Represents a validated content file (Markdown or JSON) within a collection.

**Properties**:
- `Id` (string, required): Unique identifier (slug from front-matter or filename)
- `CollectionName` (string, required): Name of parent collection
- `FilePath` (string, required): Absolute path to content file
- `Slug` (string, required): URL-friendly identifier (from front-matter or filename)
- `Content` (string, required): Raw body content (for Markdown) or null (for JSON)
- `FrontMatter` (Dictionary<string, object>, required): Parsed front-matter or JSON data
- `IsValid` (bool): Validation status
- `ValidationErrors` (List<ValidationError>): Errors if validation failed
- `LastModified` (DateTimeOffset): File last modified timestamp

**Validation Rules**:
- Id must be unique within collection
- Slug must be unique within collection
- Slug must match pattern: `^[a-z0-9-]+$`
- FilePath must exist and be readable
- FrontMatter must conform to collection schema
- If IsValid = false, ValidationErrors must be populated

**Relationships**:
- Belongs to one `Collection`
- Has many `ValidationError` instances (if validation failed)

**State Transitions**:
```
[New File] → [Parsing] → [Validating] → [Valid] or [Invalid]
                ↓             ↓             ↓
             [Error]       [Error]     [Cached/Served]
```

---

### ContentIdentifier

Encapsulates the logic for determining content item identifiers.

**Properties**:
- `Slug` (string, required): Explicit slug from front-matter
- `Filename` (string, required): Filename without extension (fallback)
- `ResolvedId` (string, computed): Final identifier (Slug ?? Filename)

**Validation Rules**:
- ResolvedId must be unique within collection
- ResolvedId must match pattern: `^[a-z0-9-]+$`

**Resolution Algorithm**:
1. Check for `slug` field in front-matter
2. If present and valid, use slug
3. If absent, use filename (without extension)
4. Normalize to lowercase and replace spaces with hyphens
5. Validate uniqueness within collection

---

### ValidationResult

Outcome of validating a content item against a schema.

**Properties**:
- `IsValid` (bool): Overall validation status
- `Errors` (List<ValidationError>): Validation errors (empty if valid)
- `Warnings` (List<ValidationWarning>): Non-blocking warnings (e.g., extra fields)
- `ValidatedAt` (DateTimeOffset): Validation timestamp

**Validation Rules**:
- If IsValid = true, Errors must be empty
- If IsValid = false, Errors must contain at least one error

**Relationships**:
- Associated with one `ContentItem`

---

### ValidationError

Detailed information about a validation failure.

**Properties**:
- `FieldName` (string, required): Name of field that failed validation
- `ErrorType` (ValidationErrorType enum, required): Type of validation error
- `Message` (string, required): Human-readable error description
- `ExpectedType` (string, optional): Expected data type
- `ActualValue` (object, optional): Actual value that failed validation

**ValidationErrorType Enum**:
```csharp
public enum ValidationErrorType
{
    RequiredFieldMissing,
    TypeMismatch,
    PatternMismatch,
    OutOfRange,
    InvalidFormat,
    InvalidEnum
}
```

---

### ValidationWarning

Non-blocking warnings about content quality.

**Properties**:
- `FieldName` (string, optional): Field that triggered warning
- `WarningType` (ValidationWarningType enum, required): Type of warning
- `Message` (string, required): Human-readable warning description

**ValidationWarningType Enum**:
```csharp
public enum ValidationWarningType
{
    ExtraField,      // Field not in schema (per spec: preserve but warn)
    DeprecatedField,
    MissingOptional  // Optional field commonly used but absent
}
```

---

### CollectionsConfiguration

Configuration model for loading collection definitions.

**Properties**:
- `Collections` (Dictionary<string, CollectionDefinition>): Collection definitions keyed by name
- `ContentRootPath` (string): Base path for all collections

**CollectionDefinition**:
- `Folder` (string): Relative folder path
- `Schema` (JsonSchema): Schema definition as JSON

**Example Configuration (collections.json)**:
```json
{
  "contentRootPath": "content",
  "collections": {
    "blog": {
      "folder": "blog",
      "schema": {
        "type": "object",
        "properties": {
          "title": { "type": "string", "minLength": 1 },
          "author": { "type": "string" },
          "publishDate": { "type": "string", "format": "date" },
          "tags": {
            "type": "array",
            "items": { "type": "string" }
          }
        },
        "required": ["title", "publishDate"]
      }
    }
  }
}
```

---

## Query Model

### QueryExpression

Parsed representation of an OData-like query.

**Properties**:
- `Filter` (FilterExpression, optional): $filter clause
- `OrderBy` (List<OrderByClause>, optional): $orderby clauses
- `Top` (int?, optional): $top (page size)
- `Skip` (int?, optional): $skip (offset)
- `Select` (List<string>, optional): $select (field projection)

---

### FilterExpression

Abstract base for filter expressions (Composite pattern).

**Subtypes**:
- `ComparisonExpression`: field op value (e.g., author eq 'John')
- `LogicalExpression`: left AND/OR right
- `NotExpression`: NOT expression

**ComparisonOperator Enum**:
```csharp
public enum ComparisonOperator
{
    Equal,          // eq
    NotEqual,       // ne
    GreaterThan,    // gt
    LessThan,       // lt
    GreaterOrEqual, // ge
    LessOrEqual,    // le
    Contains,       // contains
    StartsWith,     // startswith
    EndsWith        // endswith
}
```

---

### OrderByClause

Represents a single ordering clause.

**Properties**:
- `FieldName` (string, required): Field to sort by
- `Direction` (SortDirection enum, required): asc or desc

**SortDirection Enum**:
```csharp
public enum SortDirection
{
    Ascending,
    Descending
}
```

---

## Entity Relationships Diagram

```
CollectionsConfiguration
    │
    └─► Collection (1:N)
            ├─► CollectionSchema (1:1)
            │       └─► FieldDefinition (1:N)
            │
            └─► ContentItem (1:N)
                    ├─► ContentIdentifier (1:1)
                    └─► ValidationResult (1:1)
                            ├─► ValidationError (0:N)
                            └─► ValidationWarning (0:N)

QueryExpression
    ├─► FilterExpression (0:1)
    ├─► OrderByClause (0:N)
    └─► [Top, Skip, Select]
```

---

## Data Flow

### Collection Loading Flow
```
1. Load collections.json configuration
2. Parse collection definitions
3. Validate schema definitions
4. Create Collection entities
5. Register in DI container
```

### Content Validation Flow
```
1. Discover content files in collection folder
2. Parse file (front-matter + content)
3. Extract identifier (slug or filename)
4. Validate front-matter against schema
5. Create ContentItem with ValidationResult
6. Cache validated item
```

### Query Execution Flow
```
1. Parse query string to QueryExpression
2. Validate field names against schema
3. Apply filter to content items
4. Apply sorting
5. Apply pagination (skip/top)
6. Project fields (select)
7. Return results
```

---

## Indexes

For performance, maintain in-memory indexes on:

1. **Primary Index**: `CollectionName → List<ContentItem>`
2. **Slug Index**: `(CollectionName, Slug) → ContentItem` (unique)
3. **Field Indexes** (created on-demand for filtered fields):
   - Author → List<ContentItem>
   - PublishDate → List<ContentItem> (sorted)
   - Tags → List<ContentItem> (multi-value)

Indexes are invalidated when:
- Content file changes
- Collection schema changes
- Cache eviction

---

## Validation Rules Summary

| Entity | Key Validation Rules |
|--------|---------------------|
| Collection | Name unique, lowercase-hyphenated; folder exists |
| CollectionSchema | Valid JSON Schema; at least one property |
| FieldDefinition | Type constraints match (MinLength for String, etc.) |
| ContentItem | Slug unique; FrontMatter conforms to schema |
| ValidationResult | IsValid = false implies Errors.Count > 0 |
| QueryExpression | Field names exist in schema; operators valid for types |
