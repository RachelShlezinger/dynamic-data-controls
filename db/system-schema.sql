-- ============================================================
-- Dynamic Data Controls ג€” System Database Schema
-- ============================================================
-- Requirements: 1.2, 4.3, 6.1, 6.2, 6.3, 6.4
--
-- Creates all system tables required by the Dynamic Data
-- Controls application:
--   Controls            ג€” control definitions
--   BusinessEntities    ג€” business-name ג†’ table-name mapping
--   BusinessFields      ג€” business-name ג†’ column-name mapping
--   EntityRelationships ג€” JOIN relationships between entities
--   ControlExecutions   ג€” audit log of every control run
--
-- Collation note: Name / FriendlyName columns use
-- Hebrew_CI_AS (Case-Insensitive, Accent-Sensitive) so that
-- uniqueness checks are case-insensitive and Hebrew text sorts
-- correctly, regardless of the database-level collation.
-- ============================================================

-- ============================================================
-- Optional: create and select the system database.
-- Uncomment these lines when running against a fresh server.
-- ============================================================
-- CREATE DATABASE DynamicDataControlsDB
--
-- GO
-- USE DynamicDataControlsDB;
-- GO

-- ============================================================
-- Drop tables in reverse dependency order so FK constraints
-- do not block the drop.  Safe to re-run on a development
-- environment; remove this section for production migrations.
-- ============================================================
IF OBJECT_ID('dbo.ControlExecutions',   'U') IS NOT NULL DROP TABLE dbo.ControlExecutions;
IF OBJECT_ID('dbo.EntityRelationships', 'U') IS NOT NULL DROP TABLE dbo.EntityRelationships;
IF OBJECT_ID('dbo.BusinessFields',      'U') IS NOT NULL DROP TABLE dbo.BusinessFields;
IF OBJECT_ID('dbo.BusinessEntities',    'U') IS NOT NULL DROP TABLE dbo.BusinessEntities;
IF OBJECT_ID('dbo.Controls',            'U') IS NOT NULL DROP TABLE dbo.Controls;
GO

-- ============================================================
-- Controls
-- Stores every control definition together with its condition
-- set, serialised as JSON.
--
-- Requirement 1.2: each control must have an Id, Name,
--   Description, DefinitionJson, CreatedAt, UpdatedAt.
-- ============================================================
CREATE TABLE dbo.Controls
(
    -- Identity surrogate key
    Id              INT             NOT NULL IDENTITY(1,1),

    -- Human-readable name, max 100 chars.
    -- Hebrew_CI_AS ensures case-insensitive uniqueness: "׳‘׳§׳¨׳” ׳"
    -- and "׳‘׳§׳¨׳” ׳" are treated as the same name.
    Name            NVARCHAR(100)   NOT NULL

    -- Optional free-text description, max 500 chars (Req 1.1).
    Description     NVARCHAR(500)   NULL,

    -- Full condition definition stored as a JSON document.
    -- Format matches ControlDefinition in the design document.
    DefinitionJson  NVARCHAR(MAX)   NOT NULL,

    -- Timestamps (UTC).  CreatedAt is set once at INSERT;
    -- UpdatedAt must be refreshed by the application on every
    -- UPDATE (or via a trigger if preferred).
    CreatedAt       DATETIME2(7)    NOT NULL
                        CONSTRAINT DF_Controls_CreatedAt DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2(7)    NOT NULL
                        CONSTRAINT DF_Controls_UpdatedAt DEFAULT GETUTCDATE(),

    -- Primary key
    CONSTRAINT PK_Controls
        PRIMARY KEY CLUSTERED (Id),

    -- Unique control name (case-insensitive via Hebrew_CI_AS collation)
    -- Requirement 1.8: duplicate name check must be case-insensitive.
    CONSTRAINT UQ_Controls_Name
        UNIQUE (Name)
);
GO

-- Non-clustered index to speed up the case-insensitive
-- LIKE search on Name and the duplicate-name check.
CREATE NONCLUSTERED INDEX IX_Controls_Name
    ON dbo.Controls (Name ASC);
GO

-- ============================================================
-- BusinessEntities
-- Maps a user-friendly business name to the technical table
-- name in the target (business data) database.
--
-- Requirement 6.1: FriendlyName 1-100 chars, unique in system.
-- ============================================================
CREATE TABLE dbo.BusinessEntities
(
    Id              INT             NOT NULL IDENTITY(1,1),

    -- Friendly display name shown in the UI (e.g. "׳¢׳•׳‘׳“").
    -- Hebrew_CI_AS collation provides case-insensitive uniqueness.
    -- Requirement 6.8: duplicates rejected (case-insensitive).
    FriendlyName    NVARCHAR(100)   NOT NULL

    -- Actual table name in the target DB (e.g. "tbl_employees").
    -- Requirement 6.5: validated against INFORMATION_SCHEMA at
    -- the application layer before INSERT.
    TableName       NVARCHAR(200)   NOT NULL,

    -- Optional description of the business entity.
    Description     NVARCHAR(500)   NULL,

    CONSTRAINT PK_BusinessEntities
        PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT UQ_BusinessEntities_FriendlyName
        UNIQUE (FriendlyName)
);
GO

-- ============================================================
-- BusinessFields
-- Maps a user-friendly field name to the technical column name
-- within a specific BusinessEntity.
--
-- Requirement 6.2: FriendlyName 1-100 chars, unique within entity.
-- Requirement 6.3: DataType limited to the four allowed values.
-- ============================================================
CREATE TABLE dbo.BusinessFields
(
    Id              INT             NOT NULL IDENTITY(1,1),

    -- Parent entity.  CASCADE DELETE: removing an entity also
    -- removes all its field mappings (safe ג€” entities own fields).
    EntityId        INT             NOT NULL,

    -- Friendly display name shown in the condition builder
    -- (e.g. "׳’׳™׳", "׳©׳›׳¨_׳--׳•׳“׳©׳™").
    FriendlyName    NVARCHAR(100)   NOT NULL

    -- Actual column name in the target DB (e.g. "employee_age").
    ColumnName      NVARCHAR(200)   NOT NULL,

    -- Data type governs which operators the UI presents and how
    -- the QueryBuilder generates SQL.
    -- Allowed: Number | String | Date | Boolean
    DataType        NVARCHAR(20)    NOT NULL,

    -- Optional description of the field (up to 200 chars per design).
    -- Shown in the condition builder to help the user choose the
    -- right field (Requirement 2.2: up to 200 chars).
    Description     NVARCHAR(200)   NULL,

    CONSTRAINT PK_BusinessFields
        PRIMARY KEY CLUSTERED (Id),

    -- Cascading FK: when a BusinessEntity is deleted, all its
    -- fields are removed automatically.
    CONSTRAINT FK_BusinessFields_BusinessEntities
        FOREIGN KEY (EntityId)
        REFERENCES dbo.BusinessEntities (Id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    -- (EntityId, FriendlyName) uniqueness ensures that field names
    -- are unique within each entity (but the same name may appear
    -- in different entities).
    -- Requirement 6.2: unique per entity, case-insensitive.
    CONSTRAINT UQ_BusinessFields_EntityId_FriendlyName
        UNIQUE (EntityId, FriendlyName),

    -- Restrict DataType to exactly the four allowed values.
    -- Requirement 6.3.
    CONSTRAINT CK_BusinessFields_DataType
        CHECK (DataType IN ('Number', 'String', 'Date', 'Boolean'))
);
GO

-- Index for the common lookup "give me all fields for entity X"
-- used by the condition builder and by the QueryBuilder.
CREATE NONCLUSTERED INDEX IX_BusinessFields_EntityId
    ON dbo.BusinessFields (EntityId ASC);
GO

-- ============================================================
-- EntityRelationships
-- Stores the JOIN definition between two BusinessEntities so
-- the QueryBuilderService can produce multi-entity queries.
--
-- Requirement 6.4: relationship stores source entity, target
--   entity, source join field, and target join field.
-- Requirement 6.6: application validates both fields exist
--   before inserting a relationship.
-- ============================================================
CREATE TABLE dbo.EntityRelationships
(
    Id              INT             NOT NULL IDENTITY(1,1),

    -- The "left" entity in the JOIN (the driving table).
    SourceEntityId  INT             NOT NULL,

    -- The "right" entity in the JOIN (the joined table).
    TargetEntityId  INT             NOT NULL,

    -- The column in the source entity used in the ON clause.
    SourceFieldId   INT             NOT NULL,

    -- The column in the target entity used in the ON clause.
    TargetFieldId   INT             NOT NULL,

    CONSTRAINT PK_EntityRelationships
        PRIMARY KEY CLUSTERED (Id),

    -- NO ACTION on the entity FKs: SQL Server cannot CASCADE
    -- DELETE through multiple paths to the same table (would
    -- create a cycle with BusinessFields CASCADE above).
    -- The application or a stored procedure must remove
    -- relationships before deleting an entity.
    CONSTRAINT FK_EntityRelationships_SourceEntity
        FOREIGN KEY (SourceEntityId)
        REFERENCES dbo.BusinessEntities (Id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT FK_EntityRelationships_TargetEntity
        FOREIGN KEY (TargetEntityId)
        REFERENCES dbo.BusinessEntities (Id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    -- NO ACTION on the field FKs for the same cycle-prevention
    -- reason.  Application validates field existence before save.
    CONSTRAINT FK_EntityRelationships_SourceField
        FOREIGN KEY (SourceFieldId)
        REFERENCES dbo.BusinessFields (Id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT FK_EntityRelationships_TargetField
        FOREIGN KEY (TargetFieldId)
        REFERENCES dbo.BusinessFields (Id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

-- Composite index for the most common query pattern:
-- "find the relationship between entity A and entity B".
CREATE NONCLUSTERED INDEX IX_EntityRelationships_SourceTarget
    ON dbo.EntityRelationships (SourceEntityId ASC, TargetEntityId ASC);
GO

-- ============================================================
-- ControlExecutions
-- Immutable audit log ג€” one row per control run.
-- The application INSERTs with Status='Running', then UPDATEs
-- Status, AnomalyCount, RunTimeMs when the run ends.
--
-- Requirement 4.3: every run must record ControlId, timestamp,
--   anomaly count, run time, and status.
-- ============================================================
CREATE TABLE dbo.ControlExecutions
(
    Id              INT             NOT NULL IDENTITY(1,1),

    -- The control that was executed.
    -- CASCADE DELETE: removing a control also removes its history.
    ControlId       INT             NOT NULL,

    -- UTC timestamp of when the run started.
    ExecutedAt      DATETIME2(7)    NOT NULL
                        CONSTRAINT DF_ControlExecutions_ExecutedAt DEFAULT GETUTCDATE(),

    -- Number of anomalous records found.  NULL while running.
    AnomalyCount    INT             NULL,

    -- Wall-clock execution time in milliseconds.  NULL while running.
    RunTimeMs       INT             NULL,

    -- Lifecycle state of the run.
    -- Running   ג†’ currently executing
    -- Success   ג†’ completed normally
    -- Error     ג†’ failed with an exception
    -- Cancelled ג†’ aborted by the user (Requirement 4.9)
    Status          NVARCHAR(20)    NOT NULL,

    -- Full technical error details (never exposed to the UI).
    -- Requirement 4.6 / 8.4: log internally, return generic message.
    ErrorMessage    NVARCHAR(MAX)   NULL,

    CONSTRAINT PK_ControlExecutions
        PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT FK_ControlExecutions_Controls
        FOREIGN KEY (ControlId)
        REFERENCES dbo.Controls (Id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    -- Status must be one of the four allowed lifecycle values.
    CONSTRAINT CK_ControlExecutions_Status
        CHECK (Status IN ('Running', 'Success', 'Error', 'Cancelled'))
);
GO

-- Index for the two most frequent access patterns:
-- (a) "latest execution for control X" (used in the controls list)
-- (b) "full history for control X, newest first"
CREATE NONCLUSTERED INDEX IX_ControlExecutions_ControlId_ExecutedAt
    ON dbo.ControlExecutions (ControlId ASC, ExecutedAt DESC)
    INCLUDE (Status, AnomalyCount, RunTimeMs);
GO

-- ============================================================
-- End of system-schema.sql
-- ============================================================
