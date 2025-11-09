# Tasks: Content Collections

**Input**: Design documents from `/specs/002-content-collections/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: Tests are REQUIRED per Constitution Principle I (TDD - NON-NEGOTIABLE)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

## Path Conventions

Single Web API project structure:
- Models: `src/Markdn.Api/Models/`
- Services: `src/Markdn.Api/Services/`
- Configuration: `src/Markdn.Api/Configuration/`
- Endpoints: `src/Markdn.Api/Endpoints/`
- Tests: `tests/Markdn.Api.Tests.{Unit|Integration|Contract}/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and dependency installation

- [X] T001 Add NJsonSchema NuGet package (version 11.x) to src/Markdn.Api/Markdn.Api.csproj
- [X] T002 [P] Create Models directory structure: src/Markdn.Api/Models/
- [X] T003 [P] Create Services directory structure: src/Markdn.Api/Services/
- [X] T004 [P] Create Validation directory structure: src/Markdn.Api/Validation/
- [X] T005 [P] Create Querying directory structure: src/Markdn.Api/Querying/
- [X] T006 [P] Create Endpoints directory structure: src/Markdn.Api/Endpoints/
- [X] T007 [P] Create HostedServices directory structure: src/Markdn.Api/HostedServices/
- [X] T008 [P] Create test project directories per plan.md structure

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core models and configuration that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T009 [P] Create FieldType enum in src/Markdn.Api/Models/FieldType.cs
- [X] T010 [P] Create ValidationErrorType enum in src/Markdn.Api/Models/ValidationErrorType.cs
- [X] T011 [P] Create ValidationWarningType enum in src/Markdn.Api/Models/ValidationWarningType.cs
- [X] T012 [P] Create ComparisonOperator enum in src/Markdn.Api/Models/ComparisonOperator.cs
- [X] T013 [P] Create SortDirection enum in src/Markdn.Api/Models/SortDirection.cs
- [X] T014 Create FieldDefinition model in src/Markdn.Api/Models/FieldDefinition.cs
- [X] T015 Create CollectionSchema model in src/Markdn.Api/Models/CollectionSchema.cs (depends on T014)
- [X] T016 Create Collection model in src/Markdn.Api/Models/Collection.cs (depends on T015)
- [X] T017 [P] Create ValidationError model in src/Markdn.Api/Models/ValidationError.cs
- [X] T018 [P] Create ValidationWarning model in src/Markdn.Api/Models/ValidationWarning.cs
- [X] T019 Create ValidationResult model in src/Markdn.Api/Models/ValidationResult.cs (depends on T017, T018)
- [X] T020 [P] Create ContentIdentifier model in src/Markdn.Api/Models/ContentIdentifier.cs
- [X] T021 Create ContentItem model in src/Markdn.Api/Models/ContentItem.cs (depends on T019, T020)
- [X] T022 Create CollectionsConfiguration model in src/Markdn.Api/Configuration/CollectionsConfiguration.cs
- [X] T023 Create CollectionsOptions model in src/Markdn.Api/Configuration/CollectionsOptions.cs
- [X] T024 Register IOptions<CollectionsOptions> in src/Markdn.Api/Program.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Define Collection Schema (Priority: P1) 🎯 MVP

**Goal**: Enable defining collections with schemas in collections.json configuration file

**Independent Test**: Create a collections.json file with schema definitions, start application, verify schemas are loaded and accessible via API

### Tests for User Story 1 (TDD - Write First!)

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T025 [P] [US1] Unit test for CollectionLoader loading collections.json in tests/Markdn.Api.Tests.Unit/Services/CollectionLoaderTests.cs
- [ ] T026 [P] [US1] Unit test for CollectionLoader parsing JSON Schema definitions in tests/Markdn.Api.Tests.Unit/Services/CollectionLoaderTests.cs
- [ ] T027 [P] [US1] Unit test for CollectionLoader handling missing configuration file in tests/Markdn.Api.Tests.Unit/Services/CollectionLoaderTests.cs
- [ ] T028 [P] [US1] Contract test for GET /api/collections endpoint in tests/Markdn.Api.Tests.Contract/Endpoints/CollectionsEndpointsTests.cs
- [ ] T029 [P] [US1] Contract test for GET /api/collections/{name} endpoint in tests/Markdn.Api.Tests.Contract/Endpoints/CollectionsEndpointsTests.cs
- [ ] T030 [P] [US1] Integration test for loading and retrieving collection metadata in tests/Markdn.Api.Tests.Integration/CollectionWorkflowTests.cs

### Implementation for User Story 1

- [ ] T031 [US1] Create ICollectionLoader interface in src/Markdn.Api/Services/ICollectionLoader.cs
- [ ] T032 [US1] Implement CollectionLoader service in src/Markdn.Api/Services/CollectionLoader.cs (loads collections.json, parses schemas)
- [ ] T033 [US1] Add structured logging to CollectionLoader with ILogger in src/Markdn.Api/Services/CollectionLoader.cs
- [ ] T034 [US1] Create CollectionsEndpoints minimal API in src/Markdn.Api/Endpoints/CollectionsEndpoints.cs (GET /api/collections, GET /api/collections/{name})
- [ ] T035 [US1] Register ICollectionLoader as singleton in src/Markdn.Api/Program.cs
- [ ] T036 [US1] Map CollectionsEndpoints in src/Markdn.Api/Program.cs
- [ ] T037 [US1] Add error handling for invalid JSON schemas in src/Markdn.Api/Services/CollectionLoader.cs
- [ ] T038 [US1] Add timeout handling for file I/O operations in src/Markdn.Api/Services/CollectionLoader.cs
- [ ] T039 [US1] Verify all tests pass (T025-T030)

**Checkpoint**: Collections can be defined in configuration file and queried via API

---

## Phase 4: User Story 2 - Validate Content Against Schema (Priority: P1) 🎯 MVP

**Goal**: Automatically validate content files against collection schemas at startup and runtime

**Independent Test**: Add content files (valid and invalid) to a collection folder, start application, verify valid files are accepted and invalid files are rejected with clear error messages

### Tests for User Story 2 (TDD - Write First!)

- [ ] T040 [P] [US2] Unit test for SchemaValidator validating required fields in tests/Markdn.Api.Tests.Unit/Services/SchemaValidatorTests.cs
- [ ] T041 [P] [US2] Unit test for SchemaValidator validating field types in tests/Markdn.Api.Tests.Unit/Services/SchemaValidatorTests.cs
- [ ] T042 [P] [US2] Unit test for SchemaValidator handling optional fields in tests/Markdn.Api.Tests.Unit/Services/SchemaValidatorTests.cs
- [ ] T043 [P] [US2] Unit test for SchemaValidator handling extra fields with warnings in tests/Markdn.Api.Tests.Unit/Services/SchemaValidatorTests.cs
- [ ] T044 [P] [US2] Unit test for ContentItemValidator creating ValidationResult in tests/Markdn.Api.Tests.Unit/Validation/ContentItemValidatorTests.cs
- [ ] T045 [P] [US2] Integration test for eager validation at startup in tests/Markdn.Api.Tests.Integration/ContentRenderingTests.cs
- [ ] T046 [P] [US2] Integration test for lazy validation at runtime in tests/Markdn.Api.Tests.Integration/ContentRenderingTests.cs
- [ ] T047 [P] [US2] Contract test for GET /api/collections/{name}/validate endpoint in tests/Markdn.Api.Tests.Contract/Endpoints/CollectionsEndpointsTests.cs

### Implementation for User Story 2

- [ ] T048 [US2] Create ISchemaValidator interface in src/Markdn.Api/Services/ISchemaValidator.cs
- [ ] T049 [US2] Implement SchemaValidator service in src/Markdn.Api/Services/SchemaValidator.cs (uses NJsonSchema for validation)
- [ ] T050 [US2] Add JSON Schema compilation caching in src/Markdn.Api/Services/SchemaValidator.cs
- [ ] T051 [US2] Create ContentItemValidator for front-matter validation in src/Markdn.Api/Validation/ContentItemValidator.cs
- [ ] T052 [US2] Implement ValidationResult builder with errors and warnings in src/Markdn.Api/Services/SchemaValidator.cs
- [ ] T053 [US2] Create CollectionValidationService hosted service in src/Markdn.Api/HostedServices/CollectionValidationService.cs (eager validation at startup)
- [ ] T054 [US2] Add lazy validation on content access in src/Markdn.Api/Services/CollectionService.cs
- [ ] T055 [US2] Add extra field detection and warning logging in src/Markdn.Api/Services/SchemaValidator.cs
- [ ] T056 [US2] Register ISchemaValidator as scoped service in src/Markdn.Api/Program.cs
- [ ] T057 [US2] Register CollectionValidationService as hosted service in src/Markdn.Api/Program.cs
- [ ] T058 [US2] Add GET /api/collections/{name}/validate endpoint to CollectionsEndpoints in src/Markdn.Api/Endpoints/CollectionsEndpoints.cs
- [ ] T059 [US2] Add structured logging for validation operations with ILogger in src/Markdn.Api/Services/SchemaValidator.cs
- [ ] T060 [US2] Add validation performance metrics logging in src/Markdn.Api/HostedServices/CollectionValidationService.cs
- [ ] T061 [US2] Verify all tests pass (T040-T047)

**Checkpoint**: Content files are validated against schemas with clear error messages

---

## Phase 5: User Story 3 - Query Collection Content with Type Safety (Priority: P2)

**Goal**: Retrieve validated content items from collections with guarantee that all items conform to schema

**Independent Test**: Query a collection and verify all returned items have required fields populated and match schema structure

### Tests for User Story 3 (TDD - Write First!)

- [ ] T062 [P] [US3] Unit test for CollectionService listing all items in tests/Markdn.Api.Tests.Unit/Services/CollectionServiceTests.cs
- [ ] T063 [P] [US3] Unit test for CollectionService getting single item by slug in tests/Markdn.Api.Tests.Unit/Services/CollectionServiceTests.cs
- [ ] T064 [P] [US3] Unit test for ContentIdentifier slug resolution logic in tests/Markdn.Api.Tests.Unit/Models/ContentIdentifierTests.cs
- [ ] T065 [P] [US3] Contract test for GET /api/collections/{name}/items endpoint in tests/Markdn.Api.Tests.Contract/Endpoints/CollectionsEndpointsTests.cs
- [ ] T066 [P] [US3] Contract test for GET /api/collections/{name}/items/{id} endpoint in tests/Markdn.Api.Tests.Contract/Endpoints/CollectionsEndpointsTests.cs
- [ ] T067 [P] [US3] Integration test for querying collection with validated content in tests/Markdn.Api.Tests.Integration/CollectionQueryTests.cs

### Implementation for User Story 3

- [ ] T068 [US3] Create ICollectionService interface in src/Markdn.Api/Services/ICollectionService.cs
- [ ] T069 [US3] Implement CollectionService with GetAllItemsAsync method in src/Markdn.Api/Services/CollectionService.cs
- [ ] T070 [US3] Implement CollectionService GetItemByIdAsync method in src/Markdn.Api/Services/CollectionService.cs
- [ ] T071 [US3] Add content item caching with IMemoryCache in src/Markdn.Api/Services/CollectionService.cs
- [ ] T072 [US3] Implement ContentIdentifier resolution (slug from front-matter or filename) in src/Markdn.Api/Services/CollectionService.cs
- [ ] T073 [US3] Add slug uniqueness validation within collection in src/Markdn.Api/Services/CollectionService.cs
- [ ] T074 [US3] Add GET /api/collections/{name}/items endpoint to CollectionsEndpoints in src/Markdn.Api/Endpoints/CollectionsEndpoints.cs
- [ ] T075 [US3] Add GET /api/collections/{name}/items/{id} endpoint to CollectionsEndpoints in src/Markdn.Api/Endpoints/CollectionsEndpoints.cs
- [ ] T076 [US3] Register ICollectionService as scoped service in src/Markdn.Api/Program.cs
- [ ] T077 [US3] Add structured logging for query operations in src/Markdn.Api/Services/CollectionService.cs
- [ ] T078 [US3] Add 404 error handling for missing collections and items in src/Markdn.Api/Endpoints/CollectionsEndpoints.cs
- [ ] T079 [US3] Verify all tests pass (T062-T067)

**Checkpoint**: Collections can be queried with type-safe validated content

---

## Phase 6: User Story 4 - Advanced Query and Filtering (Priority: P2)

**Goal**: Support OData-like query syntax for filtering, sorting, pagination, and field selection

**Independent Test**: Execute queries with various filters, sorts, and pagination parameters, verify correct subset of content is returned

### Tests for User Story 4 (TDD - Write First!)

- [ ] T080 [P] [US4] Unit test for QueryParser parsing $filter expressions in tests/Markdn.Api.Tests.Unit/Querying/QueryParserTests.cs
- [ ] T081 [P] [US4] Unit test for QueryParser parsing $orderby expressions in tests/Markdn.Api.Tests.Unit/Querying/QueryParserTests.cs
- [ ] T082 [P] [US4] Unit test for QueryParser parsing $top and $skip in tests/Markdn.Api.Tests.Unit/Querying/QueryParserTests.cs
- [ ] T083 [P] [US4] Unit test for QueryParser parsing $select in tests/Markdn.Api.Tests.Unit/Querying/QueryParserTests.cs
- [ ] T084 [P] [US4] Unit test for QueryParser handling invalid query syntax in tests/Markdn.Api.Tests.Unit/Querying/QueryParserTests.cs
- [ ] T085 [P] [US4] Unit test for QueryExecutor applying filter expressions in tests/Markdn.Api.Tests.Unit/Querying/QueryExecutorTests.cs
- [ ] T086 [P] [US4] Unit test for QueryExecutor applying sorting in tests/Markdn.Api.Tests.Unit/Querying/QueryExecutorTests.cs
- [ ] T087 [P] [US4] Unit test for QueryExecutor applying pagination in tests/Markdn.Api.Tests.Unit/Querying/QueryExecutorTests.cs
- [ ] T088 [P] [US4] Unit test for QueryExecutor applying field selection in tests/Markdn.Api.Tests.Unit/Querying/QueryExecutorTests.cs
- [ ] T089 [P] [US4] Integration test for filtering by field values in tests/Markdn.Api.Tests.Integration/CollectionQueryTests.cs
- [ ] T090 [P] [US4] Integration test for sorting and pagination in tests/Markdn.Api.Tests.Integration/CollectionQueryTests.cs
- [ ] T091 [P] [US4] Integration test for combined filters with logical operators in tests/Markdn.Api.Tests.Integration/CollectionQueryTests.cs

### Implementation for User Story 4

- [ ] T092 [P] [US4] Create QueryExpression model in src/Markdn.Api/Querying/QueryExpression.cs
- [ ] T093 [P] [US4] Create FilterExpression abstract base class in src/Markdn.Api/Querying/FilterExpression.cs
- [ ] T094 [P] [US4] Create ComparisonExpression model in src/Markdn.Api/Querying/ComparisonExpression.cs
- [ ] T095 [P] [US4] Create LogicalExpression model in src/Markdn.Api/Querying/LogicalExpression.cs
- [ ] T096 [P] [US4] Create OrderByClause model in src/Markdn.Api/Querying/OrderByClause.cs
- [ ] T097 [US4] Create IQueryParser interface in src/Markdn.Api/Querying/IQueryParser.cs
- [ ] T098 [US4] Implement QueryParser for $filter parsing in src/Markdn.Api/Querying/QueryParser.cs
- [ ] T099 [US4] Add $orderby parsing to QueryParser in src/Markdn.Api/Querying/QueryParser.cs
- [ ] T100 [US4] Add $top, $skip, $select parsing to QueryParser in src/Markdn.Api/Querying/QueryParser.cs
- [ ] T101 [US4] Add query syntax validation against schema in src/Markdn.Api/Querying/QueryParser.cs
- [ ] T102 [US4] Implement QueryExecutor for applying filters in src/Markdn.Api/Querying/QueryExecutor.cs
- [ ] T103 [US4] Add sorting logic to QueryExecutor in src/Markdn.Api/Querying/QueryExecutor.cs
- [ ] T104 [US4] Add pagination logic to QueryExecutor in src/Markdn.Api/Querying/QueryExecutor.cs
- [ ] T105 [US4] Add field selection logic to QueryExecutor in src/Markdn.Api/Querying/QueryExecutor.cs
- [ ] T106 [US4] Add query result caching with cache key hashing in src/Markdn.Api/Services/CollectionService.cs
- [ ] T107 [US4] Integrate QueryParser and QueryExecutor in CollectionService in src/Markdn.Api/Services/CollectionService.cs
- [ ] T108 [US4] Add query parameter validation to GET /api/collections/{name}/items endpoint in src/Markdn.Api/Endpoints/CollectionsEndpoints.cs
- [ ] T109 [US4] Register IQueryParser as scoped service in src/Markdn.Api/Program.cs
- [ ] T110 [US4] Add structured logging for query parsing and execution in src/Markdn.Api/Querying/QueryParser.cs
- [ ] T111 [US4] Add query performance metrics logging in src/Markdn.Api/Services/CollectionService.cs
- [ ] T112 [US4] Add 400 error handling for invalid query syntax in src/Markdn.Api/Endpoints/CollectionsEndpoints.cs
- [ ] T113 [US4] Verify all tests pass (T080-T091)

**Checkpoint**: Advanced OData-like querying is fully functional

---

## Phase 7: User Story 5 - List Available Collections (Priority: P3)

**Goal**: Discover available collections and their schemas programmatically

**Independent Test**: Define multiple collections, query the API, verify all collection names and schemas are returned

### Tests for User Story 5 (TDD - Write First!)

- [ ] T114 [P] [US5] Unit test for CollectionService listing all collections in tests/Markdn.Api.Tests.Unit/Services/CollectionServiceTests.cs
- [ ] T115 [P] [US5] Integration test for listing collections with empty configuration in tests/Markdn.Api.Tests.Integration/CollectionWorkflowTests.cs
- [ ] T116 [P] [US5] Integration test for listing multiple collections in tests/Markdn.Api.Tests.Integration/CollectionWorkflowTests.cs

### Implementation for User Story 5

- [ ] T117 [US5] Add GetAllCollectionsAsync method to ICollectionService in src/Markdn.Api/Services/ICollectionService.cs
- [ ] T118 [US5] Implement GetAllCollectionsAsync in CollectionService in src/Markdn.Api/Services/CollectionService.cs
- [ ] T119 [US5] Add collection metadata caching in src/Markdn.Api/Services/CollectionService.cs
- [ ] T120 [US5] Update GET /api/collections endpoint to use GetAllCollectionsAsync in src/Markdn.Api/Endpoints/CollectionsEndpoints.cs
- [ ] T121 [US5] Add structured logging for collection discovery in src/Markdn.Api/Services/CollectionService.cs
- [ ] T122 [US5] Verify all tests pass (T114-T116)

**Checkpoint**: All user stories are independently functional

---

## Phase 8: Runtime Content Monitoring

**Purpose**: FileSystemWatcher integration for detecting content changes at runtime

- [ ] T123 Create FileSystemWatcher configuration in src/Markdn.Api/HostedServices/CollectionValidationService.cs
- [ ] T124 Add file change event handlers (Created, Changed, Deleted) in src/Markdn.Api/HostedServices/CollectionValidationService.cs
- [ ] T125 Implement 300ms debouncing for file change events in src/Markdn.Api/HostedServices/CollectionValidationService.cs
- [ ] T126 Add cache invalidation on file changes in src/Markdn.Api/Services/CollectionService.cs
- [ ] T127 Add background revalidation for changed files in src/Markdn.Api/HostedServices/CollectionValidationService.cs
- [ ] T128 Add file watcher error handling with retry logic in src/Markdn.Api/HostedServices/CollectionValidationService.cs
- [ ] T129 Add structured logging for file change events in src/Markdn.Api/HostedServices/CollectionValidationService.cs
- [ ] T130 Integration test for file change detection in tests/Markdn.Api.Tests.Integration/FileWatchingTests.cs
- [ ] T131 Verify file watching works end-to-end

**Checkpoint**: Content changes are detected and revalidated at runtime

---

## Phase 9: Schema Change Handling

**Purpose**: Background revalidation when collection schemas are modified

- [ ] T132 Add FileSystemWatcher for collections.json in src/Markdn.Api/Services/CollectionLoader.cs
- [ ] T133 Implement schema reload on configuration change in src/Markdn.Api/Services/CollectionLoader.cs
- [ ] T134 Trigger background revalidation on schema change in src/Markdn.Api/HostedServices/CollectionValidationService.cs
- [ ] T135 Add comprehensive validation report generation in src/Markdn.Api/HostedServices/CollectionValidationService.cs
- [ ] T136 Add POST /api/collections/{name}/validate endpoint for manual validation trigger in src/Markdn.Api/Endpoints/CollectionsEndpoints.cs
- [ ] T137 Add validation job tracking and status reporting in src/Markdn.Api/Services/CollectionService.cs
- [ ] T138 Integration test for schema change revalidation in tests/Markdn.Api.Tests.Integration/FullWorkflowTests.cs
- [ ] T139 Verify schema changes trigger revalidation correctly

**Checkpoint**: Schema changes trigger automatic revalidation

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T140 [P] Add Activity Source for distributed tracing in src/Markdn.Api/Program.cs
- [ ] T141 [P] Add custom metrics for cache hit rates in src/Markdn.Api/Services/CollectionService.cs
- [ ] T142 [P] Add health check endpoint for validation status in src/Markdn.Api/Program.cs
- [ ] T143 [P] Performance profiling with BenchmarkDotNet for query operations in tests/benchmarks/
- [ ] T144 [P] Optimize query performance if profiling shows issues in src/Markdn.Api/Querying/QueryExecutor.cs
- [ ] T145 [P] Consider object pooling for ValidationResult if profiling shows benefit in src/Markdn.Api/Services/SchemaValidator.cs
- [ ] T146 [P] Update README.md with Collections feature documentation
- [ ] T147 [P] Add XML documentation comments to public APIs in src/Markdn.Api/
- [ ] T148 Code review and refactoring pass across all components
- [ ] T149 Security audit for input validation and injection risks
- [ ] T150 Run quickstart.md validation end-to-end
- [ ] T151 Final integration testing of all user stories together in tests/Markdn.Api.Tests.Integration/FullWorkflowTests.cs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion (T001-T008) - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (T009-T024)
- **User Story 2 (Phase 4)**: Depends on Foundational (T009-T024) AND User Story 1 (T025-T039)
- **User Story 3 (Phase 5)**: Depends on Foundational (T009-T024) AND User Story 2 (T040-T061)
- **User Story 4 (Phase 6)**: Depends on Foundational (T009-T024) AND User Story 3 (T062-T079)
- **User Story 5 (Phase 7)**: Depends on Foundational (T009-T024) AND User Story 1 (T025-T039)
- **Runtime Monitoring (Phase 8)**: Depends on User Story 2 (T040-T061)
- **Schema Changes (Phase 9)**: Depends on User Story 2 (T040-T061) AND Phase 8 (T123-T131)
- **Polish (Phase 10)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Foundation only - independently testable ✅
- **User Story 2 (P1)**: Requires US1 (schema loading) - independently testable ✅
- **User Story 3 (P2)**: Requires US2 (validation) - independently testable ✅
- **User Story 4 (P2)**: Requires US3 (querying) - independently testable ✅
- **User Story 5 (P3)**: Requires US1 (schema loading) - independently testable ✅

### Within Each User Story (TDD Pattern)

1. Write tests FIRST (all [P] tests can run in parallel)
2. Ensure tests FAIL (Red)
3. Implement code to pass tests (Green)
4. Refactor while keeping tests passing (Refactor)
5. Verify all tests pass before moving to next story

### Parallel Opportunities

**Setup Phase (Phase 1)**: Tasks T002-T008 can run in parallel

**Foundational Phase (Phase 2)**: 
- Enums (T009-T013) can run in parallel
- Models after enums: T014-T021 have dependencies but T017, T018, T020 can run in parallel
- Configuration (T022-T023) can run in parallel with models

**User Story Tests**: All tests within a story marked [P] can run in parallel

**Models within stories**: Models marked [P] can run in parallel

**Multiple User Stories**: If team capacity allows, US5 can be started in parallel with US2-US4 since it only depends on US1

---

## Parallel Example: User Story 2

```bash
# Write ALL tests first in parallel (TDD Red phase)
Task T040 & Task T041 & Task T042 & Task T043 & Task T044 & Task T045 & Task T046 & Task T047

# Wait for all tests to complete and FAIL

# Then implement sequentially (TDD Green phase)
Task T048 → Task T049 → Task T050 → ... → Task T061

# Verify all tests PASS
```

---

## Implementation Strategy

### Minimum Viable Product (MVP)

**MVP Scope**: User Stories 1 + 2 (Phase 3-4)
- Define collections with schemas
- Validate content against schemas
- **Result**: Type-safe content management with validation

### Incremental Delivery

1. **Release 1**: MVP (US1 + US2) - Schema definition and validation
2. **Release 2**: US3 - Basic querying without filters
3. **Release 3**: US4 - Advanced OData-like queries
4. **Release 4**: US5 + Runtime monitoring - Full feature set

### Testing Strategy

- **TDD Mandatory**: Write tests before implementation per Constitution
- **Test Levels**:
  - Unit tests: Fast, isolated component testing
  - Integration tests: End-to-end workflows
  - Contract tests: API contract verification
- **Coverage Target**: >80% for all public APIs and critical paths
- **Test Execution**: Run after every implementation task

---

## Task Summary

**Total Tasks**: 151

**Breakdown by Phase**:
- Phase 1 (Setup): 8 tasks
- Phase 2 (Foundational): 16 tasks
- Phase 3 (US1 - Define Schema): 15 tasks (6 tests + 9 implementation)
- Phase 4 (US2 - Validate Content): 22 tasks (8 tests + 14 implementation)
- Phase 5 (US3 - Query Content): 18 tasks (6 tests + 12 implementation)
- Phase 6 (US4 - Advanced Queries): 34 tasks (12 tests + 22 implementation)
- Phase 7 (US5 - List Collections): 9 tasks (3 tests + 6 implementation)
- Phase 8 (Runtime Monitoring): 9 tasks
- Phase 9 (Schema Changes): 8 tasks
- Phase 10 (Polish): 12 tasks

**Parallel Opportunities**: 47 tasks marked [P] can run in parallel within their phase

**Independent User Stories**: All 5 user stories can be independently tested and delivered

**MVP Task Count**: 39 tasks (Phase 1-4)

**Estimated Development Time**: 8-12 sessions following TDD approach
