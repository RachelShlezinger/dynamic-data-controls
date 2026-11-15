# Implementation Plan: Dynamic Data Controls

## Overview

מערכת Dynamic Data Controls מורכבת מארבע שכבות: מסד נתונים SQL Server, .NET 8 Web API (C#), שירות Python FastAPI לתשתית AI, ו-Angular 17+ Frontend עם RTL עברית. ה-tasks מחולקים לפי שכבות ומיושמים באופן מצטבר — כל שלב בונה על הקודם לו ומסתיים בקוד משולב ופעיל.

## Tasks

- [x] 1. Database Setup — Schema and Seed Data
  - [x] 1.1 Create SQL Server system database schema script
    - Write `db/system-schema.sql`: tables `Controls`, `BusinessEntities`, `BusinessFields`, `EntityRelationships`, `ControlExecutions` with all columns, constraints, indexes, and case-insensitive collation on `Name`/`FriendlyName`
    - Include `UNIQUE` constraint on `Controls.Name` and `BusinessEntities.FriendlyName` (case-insensitive), `CHECK` on `BusinessFields.DataType IN ('Number','String','Date','Boolean')`, and `CHECK` on `ControlExecutions.Status IN ('Running','Success','Error','Cancelled')`
    - _Requirements: 1.2, 4.3, 6.1, 6.2, 6.3, 6.4_

  - [x] 1.2 Create target database schema script
    - Write `db/target-schema.sql`: tables `tbl_employees`, `tbl_departments`, `tbl_contracts` with FK relationships
    - Minimum columns: `employee_id`, `employee_name`, `employee_age`, `employee_salary`, `department_id` (FK), `start_date`, `end_date` on contracts
    - _Requirements: 10.1, 10.4_

  - [x] 1.3 Create seed data script with intentional anomalies
    - Write `db/seed-data.sql`: 100+ records across all 3 target tables
    - Include at least 3 employee rows where `employee_age < 18` AND `employee_salary > 10000`
    - Include at least 2 contract rows where `end_date < start_date`
    - Populate `BusinessEntities`, `BusinessFields`, `EntityRelationships` in the system DB for all 3 target tables with Hebrew friendly names
    - _Requirements: 10.2, 10.3, 10.5, 10.6_

  - [x] 1.4 Create demo controls seed SQL
    - Write `db/demo-controls.sql`: INSERT 4 pre-defined controls into `Controls` table with valid `DefinitionJson`
    - Include "עובדים קטינים עם שכר גבוה" (age < 18 AND salary > 10000) and "תאריך סיום מוקדם מתאריך התחלה"
    - Ensure all 4 controls are executable without additional configuration after running schema and seed scripts
    - _Requirements: 10.3, 10.5, 10.6_

- [x] 2. .NET Web API — Foundation
  - [x] 2.1 Initialize .NET 8 Web API project structure
    - Create solution and project with folders: `Controllers/`, `Services/Interfaces/`, `Repositories/Interfaces/`, `Models/Entities/`, `Models/DTOs/`, `Models/Domain/`, `Data/`
    - Add NuGet packages: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `FsCheck.Xunit` (3.x), `Serilog.AspNetCore`, `Testcontainers.MsSql` (for integration tests)
    - _Requirements: 3.5, 8.5_

  - [x] 2.2 Create EF Core entity classes and AppDbContext
    - Write entity classes `Control`, `ControlExecution`, `BusinessEntity`, `BusinessField`, `EntityRelationship` in `Models/Entities/` matching the DB schema column names and constraints
    - Implement `AppDbContext : DbContext` with `DbSet<T>` for each entity; configure relationships, string length limits, and collation via Fluent API in `OnModelCreating`
    - _Requirements: 1.2, 4.3, 6.1_

  - [x] 2.3 Generate EF Core migrations and DB initializer
    - Run `dotnet ef migrations add InitialCreate` and verify the generated migration matches the hand-written schema
    - Write `DatabaseInitializer` class called at startup: apply pending migrations, seed `BusinessEntities`/`BusinessFields`/`EntityRelationships` and demo controls if tables are empty
    - _Requirements: 10.1, 10.3_

  - [x] 2.4 Configure JWT Bearer authentication and role authorization
    - Add `AddAuthentication().AddJwtBearer()` in `Program.cs` with configurable `Issuer`/`Audience`/`SecretKey` from `appsettings.json`
    - Define authorization policy constants `Roles.ControlsManager` and `Roles.SystemAdmin`
    - Apply `[Authorize]` base policy on all controllers; override with role policies on write endpoints
    - _Requirements: 8.6, 8.9_

  - [x] 2.5 Implement global exception middleware and security audit logger
    - Write `ExceptionHandlingMiddleware`: catch all unhandled exceptions; log `Timestamp`, `ControlId`, `UserId` (from JWT claim), `ErrorType` (enum), `TechnicalMessage`, `Sql` to `ILogger<T>`; return generic Hebrew message `"אירעה שגיאה. אנא פנה למנהל המערכת"` with no technical details
    - Write `SecurityAuditLogger` service: log Security-type events (SQL Injection attempt, DML attempt, unauthorized access) at `LogLevel.Warning` to a dedicated structured log sink
    - _Requirements: 4.6, 8.4, 8.5, 8.7_

- [x] 3. Metadata Layer (.NET)
  - [x] 3.1 Implement MetadataRepository
    - Write `MetadataRepository` implementing `IMetadataRepository`: CRUD for `BusinessEntity`, `BusinessField`, `EntityRelationship` via EF Core
    - Add `TableExistsInTargetDbAsync(string tableName)` using ADO.NET to query `INFORMATION_SCHEMA.TABLES` on the target DB connection string
    - Add `ColumnExistsInTargetDbAsync(string tableName, string columnName)` for field validation
    - _Requirements: 6.5, 6.7_

  - [x] 3.2 Implement MetadataService with validation
    - Write `MetadataService` implementing `IMetadataService`
    - Before saving a new entity: check `FriendlyName` uniqueness (case-insensitive); verify mapped table exists in target DB; throw typed exceptions on failure
    - Before saving a new field: check `FriendlyName` uniqueness within entity
    - Before saving a relationship: verify both source and target fields exist in their respective entities
    - _Requirements: 6.5, 6.6, 6.7, 6.8_

  - [x] 3.3 Implement MetadataController
    - Write `MetadataController` with: `GET /api/metadata/entities`, `POST /api/metadata/entities`, `GET /api/metadata/entities/{id}/fields`, `POST /api/metadata/entities/{id}/fields`, `GET /api/metadata/relationships`, `POST /api/metadata/relationships`
    - GET endpoints open to all authenticated users; POST endpoints restricted to `מנהל_מערכת` role
    - Return 409 on duplicate name with Hebrew message including the duplicate name; 404 on missing entity; 400 on validation failures
    - _Requirements: 2.1, 2.2, 6.1–6.8_

  - [ ]* 3.4 Write unit tests for MetadataService
    - Test: add entity with duplicate friendly name (same case) → 409 with name in message
    - Test: add entity with duplicate friendly name (different case) → 409 (case-insensitive check)
    - Test: add entity mapping to non-existent table → error with table name
    - Test: add relationship with field not belonging to entity → error with field name and entity name
    - _Requirements: 6.6, 6.7, 6.8_

- [x] 4. Controls CRUD Layer (.NET)
  - [x] 4.1 Define ControlDefinition domain models with JSON serialization
    - Write `ControlDefinition`, `ConditionDefinition` records in `Models/Domain/`
    - Define `OperatorType` and `LogicalOperator` C# enums with all values from the design operator table
    - Implement `IEquatable<ControlDefinition>` and `IEquatable<ConditionDefinition>` for structural comparison
    - Configure `System.Text.Json` serialization options (enum as string, null handling) ensuring all fields round-trip correctly
    - _Requirements: 2.8, 3.7_

  - [x] 4.2 Implement ControlsRepository
    - Write `ControlsRepository` implementing `IControlsRepository`: `GetAllAsync(string? search)`, `GetByIdAsync(int)`, `CreateAsync(Control)`, `UpdateAsync(Control)`, `DeleteAsync(int)`
    - `GetAllAsync` applies case-insensitive LIKE filter on `Name` and `Description`; returns projected DTO with Id, Name, first 100 chars of Description, CreatedAt, and last `ControlExecution.ExecutedAt`
    - _Requirements: 1.5, 1.6_

  - [x] 4.3 Implement ControlsService with business validation
    - Write `ControlsService` implementing `IControlsService`
    - Validate on create/update: name not empty/whitespace → 400; name ≤ 100 chars → 400; description ≤ 500 chars → 400; `definition.conditions` has ≥ 1 item → 400
    - Check case-insensitive name uniqueness → 409 Conflict with existing name in Hebrew message
    - _Requirements: 1.1, 1.3, 1.7, 1.8, 1.9_

  - [x] 4.4 Implement ControlsController
    - Write `ControlsController`: `GET /api/controls?search=q`, `GET /api/controls/{id}`, `POST /api/controls`, `PUT /api/controls/{id}`, `DELETE /api/controls/{id}`
    - Restrict `POST`, `PUT`, `DELETE` to `מנהל_בקרות` role; return 403 with Hebrew message on unauthorized attempt
    - Return 400 with field-level Hebrew error messages for validation failures; 409 for duplicate name
    - Return control list with "טרם הורצה" for controls without executions
    - _Requirements: 1.1–1.10, 8.6, 8.9_

  - [ ]* 4.5 Write FsCheck property test for JSON Definition Round-Trip (Property 1)
    - **Property 1: JSON Definition Round-Trip**
    - Generate arbitrary `ControlDefinition` objects with FsCheck arbitraries covering all operator types, field names, and logical operators; serialize with `JsonSerializer.Serialize`; deserialize with `JsonSerializer.Deserialize<ControlDefinition>`; assert `restored == original` using `IEquatable`
    - Run minimum 100 iterations with tag `Feature: dynamic-data-controls, Property 1`
    - **Validates: Requirements 2.8**

  - [ ]* 4.6 Write unit tests for ControlsService
    - Test: empty name string → 400 with Hebrew "שם הבקרה הוא שדה חובה"
    - Test: whitespace-only name → 400
    - Test: zero conditions → 400 with Hebrew "נדרש לפחות תנאי אחד"
    - Test: duplicate name (case-insensitive) → 409 with duplicate name in message
    - Test: valid control with 1 condition → created successfully
    - _Requirements: 1.3, 1.7, 1.8, 1.9_

- [x] 5. QueryBuilderService (.NET)
  - [x] 5.1 Implement IQueryBuilderService and core single-entity SQL builder
    - Write `IQueryBuilderService` interface with `Build(ControlDefinition definition, IReadOnlyList<BusinessEntity> metadata): QueryBuildResult` and `QueryBuildResult` record
    - Implement `QueryBuilderService`: resolve `FriendlyName` → `TableName`/`ColumnName` from metadata whitelist; assign alias `e0`; build `SELECT e0.col1, e0.col2 … FROM tbl AS e0 WHERE …`
    - Add `TOP 10001` to every SELECT for the 10,000-record enforcement
    - Map all 10 `OperatorType` values to their SQL expressions; handle `IsNull`/`IsNotNull` without parameters; name parameters `@p0`, `@p1`, …
    - Return `IsSuccess=false` with the missing name in `ErrorMessage` if any entity or field is not in the metadata whitelist
    - _Requirements: 3.1, 3.2, 3.3, 4.1_

  - [x] 5.2 Add multi-entity JOIN support to QueryBuilderService
    - Extend `Build()` to handle `additionalEntities`: assign aliases `e1`..`e9`; look up `EntityRelationship` records in the provided metadata to generate `INNER JOIN tbl AS eN ON eM.source_col = eN.target_col`
    - Support up to 10 total entities in a single query
    - If a required JOIN relationship is not found in metadata, return `IsSuccess=false` with both entity names in `ErrorMessage`
    - _Requirements: 2.5, 3.4, 3.8_

  - [x] 5.3 Add SELECT-only guard and DML/DDL rejection
    - Add a post-build validation step in `Build()`: verify the generated SQL string starts with `SELECT` (after trimming) and contains none of the keywords `INSERT`, `UPDATE`, `DELETE`, `DROP`, `TRUNCATE`, `ALTER`, `EXEC` as whole words
    - On detection: call `SecurityAuditLogger.LogDmlAttempt()`; return `IsSuccess=false` with Hebrew error message; complete within 2 seconds
    - _Requirements: 3.3, 8.1, 8.7_

  - [ ]* 5.4 Write FsCheck property test for SQL Query Determinism (Property 2)
    - **Property 2: SQL Query Determinism**
    - For any valid `ControlDefinition` and matching metadata, call `Build()` twice; assert `result1.Sql == result2.Sql` and `result1.Parameters` keys and values are identical
    - Tag: `Feature: dynamic-data-controls, Property 2`
    - **Validates: Requirements 3.7**

  - [ ]* 5.5 Write FsCheck property test for SQL SELECT-Only (Property 3)
    - **Property 3: SQL SELECT-Only**
    - For any `ControlDefinition` with valid metadata, assert `Build().Sql` starts with `SELECT` and does not contain any of `INSERT`, `UPDATE`, `DELETE`, `DROP`, `TRUNCATE`, `ALTER`, `EXEC`
    - Tag: `Feature: dynamic-data-controls, Property 3`
    - **Validates: Requirements 3.3, 8.1**

  - [ ]* 5.6 Write FsCheck property test for SQL Parameterization (Property 4)
    - **Property 4: SQL Parameterization**
    - For any `ControlDefinition` with non-null values, assert that no literal value from the conditions appears as a substring in `Build().Sql`; all values must appear only as keys `@p0`, `@p1`, … in the returned `Parameters` dictionary
    - Tag: `Feature: dynamic-data-controls, Property 4`
    - **Validates: Requirements 3.2, 8.2**

  - [ ]* 5.7 Write FsCheck property test for Unknown Metadata Returns Error (Property 5)
    - **Property 5: Unknown Metadata Always Returns Error**
    - For any `ControlDefinition` referencing an entity or field name not present in the provided metadata list, assert `Build()` returns `IsSuccess=false` and `ErrorMessage` contains the missing name; no SQL is returned
    - Tag: `Feature: dynamic-data-controls, Property 5`
    - **Validates: Requirements 3.6**

  - [ ]* 5.8 Write unit tests for QueryBuilderService
    - Test: single condition Equal → parameterized `WHERE col = @p0`
    - Test: two conditions AND → `WHERE col1 = @p0 AND col2 > @p1`
    - Test: two conditions OR → `WHERE col1 = @p0 OR col2 > @p1`
    - Test: JOIN between two entities → correct `INNER JOIN … ON` clause
    - Test: entity not in metadata → `IsSuccess=false` with entity name
    - Test: field not in entity metadata → `IsSuccess=false` with field name
    - Test: `IsNull` operator → `col IS NULL` with no parameter entry
    - _Requirements: 3.1, 3.4, 3.6, 3.8_

- [x] 6. ExecutionService (.NET)
  - [x] 6.1 Implement core ExecutionService
    - Write `ExecutionService` implementing `IExecutionService`; inject `IQueryBuilderService`, `IMetadataService`, `IControlsRepository`
    - Load `ControlDefinition` and metadata; call `QueryBuilderService.Build()`; execute via ADO.NET `SqlCommand` with `CommandTimeout = 60`
    - Enforce record cap: use `TOP 10001` from the query; if row count > 10,000 set `IsTruncated = true` and return exactly 10,000 rows
    - Save `ControlExecution` log on completion: `Status`, `AnomalyCount`, `RunTimeMs`; log full error details internally on failure; return Hebrew message to caller
    - _Requirements: 4.1, 4.2, 4.3, 5.7, 8.3_

  - [x] 6.2 Implement concurrent execution guard
    - Add `ConcurrentDictionary<int, bool>` to `ExecutionService` tracking active control IDs
    - In `ExecuteAsync`: check and add `controlId` atomically; if already present return error DTO with Hebrew message `"הרצה כבר פעילה עבור בקרה זו"`
    - Remove from dictionary on completion, cancellation, or error
    - _Requirements: 4.8_

  - [x] 6.3 Implement CancellationToken support and CancelAsync
    - Store a `CancellationTokenSource` keyed by `executionId` in an in-memory dictionary
    - Accept `CancellationToken` in `ExecuteAsync`; link it to the stored `CancellationTokenSource`
    - Implement `CancelAsync(int executionId)`: cancel the linked token; update `ControlExecution.Status` to `Cancelled`
    - After 30 seconds of execution, raise an `ExecutionLongRunningNotification` (via a SignalR hub or SSE endpoint) so the frontend can show the cancellation prompt
    - _Requirements: 4.5, 4.7, 4.9_

  - [x] 6.4 Implement ExecutionController
    - Write `ExecutionController`: `POST /api/executions { controlId }`, `DELETE /api/executions/{id}`, `GET /api/executions/{id}/results?page=1&pageSize=100`, `GET /api/controls/{id}/executions`
    - Return `201 { executionId, anomalyCount }` on success; include `isTruncated` and Hebrew truncation message in results response when applicable
    - Reject duplicate run requests with `409` and Hebrew message
    - _Requirements: 4.1–4.9, 5.1–5.9_

  - [ ]* 6.5 Write unit tests for ExecutionService
    - Test: successful run → `ControlExecution` saved with `Status=Success`, correct `AnomalyCount`, non-zero `RunTimeMs`
    - Test: same `controlId` executed twice concurrently → second call returns error with Hebrew message
    - Test: `CancelAsync` called → `ControlExecution.Status = Cancelled`
    - Test: query returns 10,001 rows → `IsTruncated=true`, exactly 10,000 rows returned
    - _Requirements: 4.1, 4.3, 4.8, 4.9, 5.7, 5.8_

- [x] 7. AI Proxy (.NET)
  - [x] 7.1 Implement AiProxyService with HTTP client, context builder, and schema validator
    - Write `AiProxyService` implementing `IAiProxyService`; inject `IMetadataService` and typed `HttpClient`
    - Build `AiContext` JSON from `MetadataService.GetAllEntitiesWithFieldsAsync()` including entity names, field names, and data types
    - POST to Python service at configurable URL with 30-second `HttpClient` timeout; on `TaskCanceledException` or non-success status code return `AiParseResult { IsSuccess=false }` with Hebrew message
    - Validate response schema: `controlName` present and non-empty; `primaryEntity` exists in metadata entities; `conditions` is a non-empty array; each condition has `fieldName`, `operator`, and `value` — return `IsSuccess=false` with list of failing field names on any violation
    - _Requirements: 7.2, 7.3, 7.7, 7.8, 7.11_

  - [x] 7.2 Implement AiProxyController
    - Write `AiProxyController`: `POST /api/ai/parse { description: string }`
    - Validate description length 10–1,000 chars before forwarding; return `400` with Hebrew message `"נדרש תיאור מפורט יותר (לפחות 10 תווים)"` if too short
    - Return `200 ControlDefinition` on success (for display and confirmation, not yet saved); `422 { error, missingFields }` on AI schema error; `503` with Hebrew message on Python service unavailability
    - _Requirements: 7.1, 7.5, 7.10_

  - [ ]* 7.3 Write FsCheck property test for AI Response Schema Validation (Property 13)
    - **Property 13: AI Response Schema Validation**
    - Generate arbitrary JSON objects with one or more of: missing `controlName`, missing `conditions`, condition missing `fieldName`/`operator`/`value`, or empty `conditions` array
    - Assert `AiProxyService.ValidateAiResponse()` always returns `IsSuccess=false` and `MissingFields` contains the name(s) of the failing field(s)
    - Tag: `Feature: dynamic-data-controls, Property 13`
    - **Validates: Requirements 7.8**

  - [ ]* 7.4 Write unit tests for AiProxyService
    - Test: fully valid AI response with entity that exists in metadata → `IsSuccess=true`, correct `ControlDefinition`
    - Test: response with empty `controlName` → `IsSuccess=false`, "controlName" in `MissingFields`
    - Test: response with empty `conditions` array → `IsSuccess=false`
    - Test: Python service returns 503 → Hebrew "שירות ה-AI אינו זמין" propagated
    - Test: `HttpClient` times out after 30s → Hebrew timeout message
    - _Requirements: 7.3, 7.8, 7.11_

- [x] 8. Python AI Service (FastAPI)
  - [x] 8.1 Set up FastAPI project structure with Pydantic models
    - Create `ai-service/` with `main.py`, `routers/parse.py`, `services/ai_parser.py`, `services/mock_ai.py`, `models/request.py`, `models/response.py`, `config.py`
    - Define Pydantic v2 models: `FieldInfo`, `EntityInfo`, `AiContext`, `ParseRequest` (description + context), `ConditionItem`, `ParseResponse` (controlName, primaryEntity, conditions)
    - Add `requirements.txt`: `fastapi>=0.111`, `uvicorn[standard]`, `pydantic>=2`, `hypothesis` (for tests), `pytest`, `httpx`
    - _Requirements: 7.7_

  - [x] 8.2 Implement MockAI engine
    - Write `mock_ai.py` with `MOCK_RESPONSES: dict[str, dict]` containing predefined responses for keywords: `"קטין"`, `"תאריך_סיום"`, and `"DEFAULT"` — matching the structure in the design document
    - Implement `find_response(description: str) -> dict`: iterate `MOCK_RESPONSES` keys; if keyword found as a substring in description return that response; fall back to `DEFAULT`
    - _Requirements: 7.4, 7.9_

  - [x] 8.3 Implement /parse router with error handling
    - Write `routers/parse.py` with `POST /parse` endpoint; call `MockAI.find_response()`; validate the returned dict against `ParseResponse` using Pydantic model validation
    - Return `200 ParseResponse` on success; `422 { "error": "...", "missingFields": [...] }` on schema validation failure; `503 { "error": "AI service unavailable" }` on internal exception
    - Register router in `main.py` and configure CORS for the .NET backend origin
    - _Requirements: 7.2, 7.3, 7.8_

  - [ ]* 8.4 Write Hypothesis property test for MockAI Round-Trip (Property 12)
    - **Property 12: MockAI Definition Round-Trip**
    - Use `@given(st.sampled_from(list(MOCK_RESPONSES.keys())))` to test each defined key
    - Assert `find_response(key)["controlName"]`, `["primaryEntity"]`, and `["conditions"]` are identical to `MOCK_RESPONSES[key]` values
    - Tag: `Feature: dynamic-data-controls, Property 12`
    - **Validates: Requirements 7.9**

  - [ ]* 8.5 Write unit tests for /parse endpoint
    - Test: POST with description containing `"קטין"` → 200 with `controlName == "עובדים קטינים עם שכר גבוה"`
    - Test: POST with unknown description → 200 with DEFAULT response
    - Test: MockAI forced to return invalid schema → 422 with `missingFields` list
    - _Requirements: 7.3, 7.8_

- [~] 9. Checkpoint — Ensure all backend and AI tests pass
  - Run all xUnit tests (`.NET`), Pytest tests (Python AI service), and verify no compilation errors. Ask the user if questions arise before proceeding to the frontend.

- [x] 10. Angular Frontend — Foundation
  - [x] 10.1 Initialize Angular 17+ project with RTL configuration
    - Create Angular project with `--standalone` components and `--routing`; set `dir="rtl"` and `lang="he"` on `<html>` in `index.html`; add `@angular/material` with `dir="rtl"` on `MatDialogConfig` and `MAT_DATE_LOCALE: 'he-IL'`
    - Configure global SCSS with RTL-compatible flex direction; install `fast-check` for property-based tests
    - _Requirements: 9.1, 9.2_

  - [x] 10.2 Create TypeScript domain interfaces and models
    - Write interfaces in `src/app/shared/models/`: `Control`, `ControlDefinition`, `ConditionDefinition`, `BusinessEntity`, `BusinessField`, `EntityRelationship`, `ControlExecution`, `ExecutionResult`, `ResultsPage`, `AiParseResponse`, `ColumnDefinition`
    - Define `OperatorType` and `LogicalOperator` string enums matching backend values
    - Define `OPERATORS_BY_TYPE: Record<DataType, OperatorType[]>` constant for all 4 data types with exact operator sets from the design
    - _Requirements: 2.3, 2.6_

  - [x] 10.3 Implement Angular core HTTP services
    - Write `ControlsService`, `MetadataService`, `ExecutionService`, `AiService` in `core/services/` using `HttpClient` with fully typed request/response generics
    - Write `NotificationService` wrapping `MatSnackBar` for Hebrew toast messages (success, error, info)
    - _Requirements: 9.6_

  - [x] 10.4 Implement AuthInterceptor and ErrorInterceptor
    - `AuthInterceptor`: clone request with `Authorization: Bearer <token>` from `localStorage`
    - `ErrorInterceptor`: catch `HttpErrorResponse`; dispatch Hebrew message via `NotificationService` within 1 second; redirect on 401; show Hebrew 403/500 messages; always propagate the error for local handling
    - _Requirements: 9.4, 8.9_

  - [x] 10.5 Set up Angular routing and AdminGuard
    - Configure `app.routes.ts` with lazy-loaded routes: `/controls`, `/controls/new`, `/controls/:id/edit`, `/controls/:id/run`, `/ai`, `/admin/metadata`
    - Write `AdminGuard` using `CanActivateFn` that reads role claim from JWT and returns `false` (redirect to `/controls`) for non-admin users
    - _Requirements: 8.6, 8.9_

- [x] 11. Angular — Shared Components
  - [x] 11.1 Implement LoadingSpinnerComponent and ErrorBannerComponent
    - `LoadingSpinnerComponent`: show `MatProgressSpinner` overlay triggered by `NotificationService.isLoading$`; activate after any HTTP request lasting > 300ms (use `interceptor` delay); dismiss on completion or error
    - `ErrorBannerComponent`: display Hebrew error message in a persistent banner that stays until the user clicks the close (×) button; support `Escape` key to dismiss; accessible with `role="alert"` and `aria-live="assertive"`
    - _Requirements: 9.3, 9.4, 9.7_

  - [x] 11.2 Implement ConditionBuilderComponent
    - `@Input() definition: ControlDefinition`, `@Output() definitionChange: EventEmitter<ControlDefinition>`
    - Render per-condition row: entity `MatSelect` → field `MatSelect` → operator `MatSelect` → value input
    - Filter entity field list when entity selection changes; filter operator list by field `dataType` using `OPERATORS_BY_TYPE`
    - Show `MatDatepicker` for `Date` type fields; show plain text input for other types
    - Validate value type on blur: mark control `invalid` and show Hebrew message on mismatch; prevent form save when any condition has type error
    - Support `+` button to add conditions (up to 20); `×` button to remove; `MatSelect` for AND/OR logical operator between conditions
    - Support full keyboard navigation: Tab/Enter between condition row elements, Escape closes any open dropdown
    - _Requirements: 2.1–2.9, 9.5, 9.7_

  - [ ]* 11.3 Write fast-check property test for Operator Set by Field Type (Property 10)
    - **Property 10: Operator Set by Field Type**
    - For each `DataType` value (`'Number'`, `'Date'`, `'String'`, `'Boolean'`), assert `OPERATORS_BY_TYPE[dataType]` is exactly the set defined in requirements: `Number`/`Date` → 8 operators; `String` → 6 operators; use `fc.constantFrom` over all data types
    - Tag: `Feature: dynamic-data-controls, Property 10`
    - **Validates: Requirements 2.3**

  - [ ]* 11.4 Write fast-check property test for JSON Definition Round-Trip (Property 1 — Angular)
    - **Property 1: JSON Definition Round-Trip (Angular)**
    - Build arbitrary `ControlDefinition` objects with `fc.record`; apply `JSON.stringify` then `JSON.parse`; assert deep equality with the original using `expect(restored).toEqual(original)` over 100 runs
    - Tag: `Feature: dynamic-data-controls, Property 1`
    - **Validates: Requirements 2.8**

- [x] 12. Angular — Controls Feature
  - [x] 12.1 Implement ControlsListComponent
    - Display `MatTable` with columns: שם, תיאור (truncated to 100 chars), תאריך יצירה, סטטוס (last run date or "טרם הורצה")
    - Connect search `<input>` to `ControlsService.search()` with 300ms `debounceTime` and `distinctUntilChanged`
    - Show "לא נמצאו בקרות התואמות את החיפוש" message (no empty table) when results are empty
    - Row action buttons: "ערוך" (navigate to edit), "הרץ" (navigate to run), keyboard-accessible via Tab/Enter
    - _Requirements: 1.5, 1.6, 1.10_

  - [x] 12.2 Implement ControlFormComponent (create and edit)
    - Accept optional `controlId` route param; if present load existing control from `ControlsService.getById()` and populate form
    - Build reactive form with Angular validators: name required + ≤ 100 chars, description ≤ 500 chars; custom `atLeastOneCondition` validator on the embedded `ConditionBuilderComponent` output
    - On server 409 response: show Hebrew warning `"בקרה בשם זה כבר קיימת: {name}"` and keep form open for name correction without resetting conditions
    - Save button calls `ControlsService.create()` or `.update()`; on success navigate to controls list
    - _Requirements: 1.1, 1.3, 1.4, 1.7, 1.8, 1.9_

  - [ ]* 12.3 Write fast-check property test for Search Filtering Correctness (Property 6)
    - **Property 6: Search Filtering Correctness**
    - Implement and test a pure `filterControls(controls: Control[], query: string): Control[]` function
    - For any array of controls and any query string, assert: (1) every returned control contains `query` (case-insensitive) in `name` or `description`; (2) no matching control is absent from results (completeness)
    - Tag: `Feature: dynamic-data-controls, Property 6`
    - **Validates: Requirements 1.6, 5.3**

- [x] 13. Angular — Execution and Results
  - [x] 13.1 Implement ControlExecutionComponent
    - "הרץ בקרה" button calls `ExecutionService.run(controlId)` and shows `LoadingSpinnerComponent`
    - After 30 seconds, show Hebrew warning `"הבקרה עדיין מורצת — ייתכן שתיקח זמן נוסף"` with a "בטל הרצה" button that calls `ExecutionService.cancel(executionId)`
    - On server 409: show Hebrew "הרצה כבר פעילה עבור בקרה זו" and disable the run button
    - On success: display anomaly count and render `ResultsTableComponent` with the execution results
    - On cancellation confirmed: show Hebrew "הרצה בוטלה" and restore run button
    - _Requirements: 4.4, 4.5, 4.7, 4.8, 4.9_

  - [x] 13.2 Implement ResultsTableComponent
    - `@Input() results: ResultsPage`, `@Input() columns: ColumnDefinition[]`; display business `friendlyName` headers (never technical column names)
    - Column header click → sort ASC; second click same header → sort DESC; third click → remove sort (or cycle back)
    - Search `<input>` with 300ms `debounceTime` filtering the displayed rows
    - `MatPaginator` with `pageSize=100`; Previous/Next navigation
    - If `results.isTruncated`, show persistent Hebrew banner: "התוצאות קוצצו ל-10,000 הרשומות הראשונות"
    - "ייצא CSV" button exports currently-filtered rows with Hebrew column headers
    - _Requirements: 5.1–5.9_

  - [ ]* 13.3 Write fast-check property test for Sort Order Correctness (Property 7)
    - **Property 7: Sort Order Correctness**
    - Implement and test `sortRecords(records, col, direction)` pure function
    - Assert: (1) after `'asc'` sort every adjacent pair satisfies `r[i][col] ≤ r[i+1][col]`; (2) after `'desc'` every pair satisfies `r[i][col] ≥ r[i+1][col]`; (3) sorting preserves all elements (no duplicates, no missing)
    - Tag: `Feature: dynamic-data-controls, Property 7`
    - **Validates: Requirements 5.4, 5.5**

  - [ ]* 13.4 Write fast-check property test for Pagination Correctness (Property 8)
    - **Property 8: Pagination Correctness**
    - Implement and test `getPage(records: T[], page: number, pageSize: number): T[]`
    - For any `records` array of length N and page index p ≥ 0, assert result equals `records.slice(p * pageSize, Math.min((p + 1) * pageSize, N))` with no duplicate or missing elements
    - Tag: `Feature: dynamic-data-controls, Property 8`
    - **Validates: Requirements 5.6**

- [x] 14. Angular — AI Interface
  - [x] 14.1 Implement AiInterfaceComponent
    - Render `<textarea>` with live character counter; min 10 chars, max 1,000 chars; keep submit button disabled and show Hebrew "נדרש תיאור מפורט יותר" if below minimum
    - On submit: call `AiService.parse(description)`, show `LoadingSpinnerComponent`
    - On success: render `ControlFormComponent` pre-populated with the returned `ControlDefinition` and an "אשר ושמור" CTA; user can edit before final save via `ControlsService.create()`
    - On 503 or timeout: show Hebrew `"שירות ה-AI אינו זמין כרגע. אנא נסה שנית"` via `NotificationService`
    - After user confirms and control is saved: navigate to controls list; control becomes available for execution immediately
    - _Requirements: 7.1, 7.5, 7.6, 7.10, 7.11_

- [x] 15. Angular — Metadata Admin
  - [x] 15.1 Implement MetadataAdminComponent (admin only)
    - Protected by `AdminGuard`; layout: left panel lists entities, right panel shows entity fields and relationships
    - "הוסף ישות" form: friendly name (1–100 chars) + technical table name; submit calls `MetadataService.addEntity()`; show Hebrew error on duplicate name or non-existent table
    - "הוסף שדה" form: friendly name, column name, data type `MatSelect` from `['Number','String','Date','Boolean']`
    - "הוסף קשר" form: source entity `MatSelect`, source field `MatSelect`, target entity `MatSelect`, target field `MatSelect`; submit calls `MetadataService.addRelationship()`; show Hebrew error on missing field
    - _Requirements: 6.1–6.8_

- [~] 16. Checkpoint — Ensure all Angular tests pass
  - Run `ng test --run` and `ng build` to verify zero errors. Ensure all fast-check property tests pass. Ask the user if questions arise before proceeding to integration wiring.

- [ ] 17. Integration and Final Wiring
  - [x] 17.1 Configure Angular proxy and verify end-to-end HTTP connectivity
    - Add `proxy.conf.json` routing `/api` to the .NET backend URL; configure CORS in .NET `Program.cs` to allow the Angular dev origin
    - Smoke-test each feature flow from the Angular app: metadata entities load, control CRUD works, execution returns results, AI parse returns definition preview
    - Fix any HTTP 404/CORS/serialization mismatches between Angular models and backend DTOs
    - _Requirements: 9.1–9.7_

  - [-] 17.2 Write integration tests (xUnit + Testcontainers)
    - **End-to-end execution**: create "עובדים קטינים עם שכר גבוה" control → run against seeded target DB → assert ≥ 3 anomaly records returned
    - **AI pipeline**: POST description with keyword "קטין" → MockAI → approve → `POST /api/controls` → `GET /api/controls/{id}` → assert `controlName`, `primaryEntity`, and `conditions` match MockAI response exactly
    - **Timeout enforcement**: craft a query exceeding 60 seconds → assert `ControlExecution.Status = Cancelled` and log entry saved
    - **Concurrent execution rejection**: fire two parallel `POST /api/executions` for same control → assert second returns `409`
    - **Record cap**: inject a query returning 10,001 rows → assert `IsTruncated=true` and exactly 10,000 rows in `results`
    - _Requirements: 4.5, 4.7, 4.8, 5.7, 5.8, 10.5, 10.6_

  - [ ]* 17.3 Write FsCheck property test for Input Validation Rejection (Property 9)
    - **Property 9: Input Validation Rejection**
    - For any `CreateControlRequest` with empty/whitespace `name`, or `definition.conditions` count = 0, assert `ControlsService.CreateAsync()` throws a validation exception and no row is inserted in `Controls`
    - For any AI description with length < 10, assert `AiProxyController` returns 400 with no call to Python service
    - Tag: `Feature: dynamic-data-controls, Property 9`
    - **Validates: Requirements 1.3, 1.7, 1.9, 7.10**

  - [ ]* 17.4 Write FsCheck property test for Type Mismatch Validation (Property 11)
    - **Property 11: Type Mismatch Validation**
    - Generate arbitrary `(DataType, value)` pairs where `value` is intentionally wrong for `DataType` (e.g., non-numeric string for `Number`, non-ISO string for `Date`)
    - Assert `QueryBuilderService.Build()` returns `IsSuccess=false` for type-mismatched conditions; assert `ControlsService.CreateAsync()` rejects such payloads with a validation error
    - Tag: `Feature: dynamic-data-controls, Property 11`
    - **Validates: Requirements 2.7**

- [~] 18. Final Checkpoint — Ensure all tests pass
  - Run all xUnit, Pytest, and Angular (`ng test --run`) test suites. Ensure all integration tests pass with Testcontainers. Ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP; core implementation tasks must always be completed
- All 13 correctness properties from `design.md` are covered: Properties 1–5, 9, 11, 13 via FsCheck (.NET); Property 12 via Hypothesis (Python); Properties 1, 6, 7, 8, 10 via fast-check (Angular)
- Checkpoint tasks (9, 16, 18) are top-level only and are not included in the dependency graph
- `QueryBuilderService` is the security perimeter: SELECT-only, fully parameterized, whitelist-validated — it must never accept raw SQL strings from any input source
- All user-facing messages (errors, warnings, status) must be in Hebrew; technical details go to logs only
- RTL configuration (`dir="rtl"`, `lang="he"`) must be in place from task 10.1 — do not defer it to later tasks

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1", "8.1", "10.1"] },
    { "id": 1, "tasks": ["1.2", "2.2", "8.2", "10.2"] },
    { "id": 2, "tasks": ["1.3", "2.3", "2.4", "8.3", "8.4", "10.3", "10.4", "10.5"] },
    { "id": 3, "tasks": ["1.4", "2.5", "3.1", "4.1", "8.5", "11.1"] },
    { "id": 4, "tasks": ["3.2", "4.2", "5.1", "7.1", "11.2"] },
    { "id": 5, "tasks": ["3.3", "3.4", "4.3", "5.2", "7.2", "7.4", "11.3", "11.4", "12.1", "14.1", "15.1", "17.4"] },
    { "id": 6, "tasks": ["4.4", "4.5", "4.6", "5.3", "5.7", "7.3", "12.2", "13.1", "13.2", "17.3"] },
    { "id": 7, "tasks": ["5.4", "5.5", "5.6", "5.8", "6.1", "12.3", "13.3", "13.4"] },
    { "id": 8, "tasks": ["6.2", "6.3"] },
    { "id": 9, "tasks": ["6.4", "6.5"] },
    { "id": 10, "tasks": ["17.1"] },
    { "id": 11, "tasks": ["17.2"] }
  ]
}
```
