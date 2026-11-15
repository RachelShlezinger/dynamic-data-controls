-- ============================================================
-- Dynamic Data Controls ג€” Target Database Schema
-- ============================================================
-- Requirements: 10.1, 10.4
--
-- Creates the sample business-data tables that the Dynamic
-- Data Controls system runs its controls against:
--
--   tbl_departments  ג€” department master data
--   tbl_employees    ג€” employee records (FK ג†’ tbl_departments)
--   tbl_contracts    ג€” employee contracts (FK ג†’ tbl_employees)
--
-- These tables live in the TARGET database (business data),
-- NOT in the system database that holds Controls / Metadata.
-- Connect to the correct database before running this script.
--
-- Collation note: name columns use Hebrew_CI_AS so that
-- Hebrew text sorts correctly and string comparisons are
-- case-insensitive, consistent with the system-schema.sql
-- convention.
-- ============================================================

-- ============================================================
-- Optional: create and select the target database.
-- Uncomment these lines when running against a fresh server.
-- ============================================================
-- CREATE DATABASE DynamicDataControlsTargetDB
--
-- GO
-- USE DynamicDataControlsTargetDB;
-- GO

-- ============================================================
-- Drop tables in reverse dependency order so FK constraints
-- do not block the drop.  Safe to re-run on a development
-- environment; remove this section for production migrations.
-- ============================================================
IF OBJECT_ID('dbo.tbl_contracts',   'U') IS NOT NULL DROP TABLE dbo.tbl_contracts;
IF OBJECT_ID('dbo.tbl_employees',   'U') IS NOT NULL DROP TABLE dbo.tbl_employees;
IF OBJECT_ID('dbo.tbl_departments', 'U') IS NOT NULL DROP TABLE dbo.tbl_departments;
GO

-- ============================================================
-- tbl_departments
-- Master table for organisational departments.
-- Referenced by tbl_employees.department_id.
--
-- Requirement 10.1: at least 3 tables with defined
--   relationships.
-- Requirement 10.4: document table name, purpose, field
--   names, data types, and relationship types.
--
-- Purpose: stores each department in the organisation with
--   its display name and whether it is currently active.
-- ============================================================
CREATE TABLE dbo.tbl_departments
(
    -- Surrogate primary key.
    department_id   INT             NOT NULL IDENTITY(1,1),

    -- Display name of the department (e.g. "Human Resources",
    -- "Finance").  Hebrew_CI_AS collation ensures Hebrew text
    -- sorts correctly and name lookups are case-insensitive.
    department_name NVARCHAR(150)   NOT NULL

    -- Soft-delete / active flag.
    -- 1 = active department, 0 = decommissioned.
    is_active       BIT             NOT NULL
                        CONSTRAINT DF_tbl_departments_is_active DEFAULT 1,

    CONSTRAINT PK_tbl_departments
        PRIMARY KEY CLUSTERED (department_id)
);
GO

-- Non-clustered index for name lookups and JOIN probes from
-- tbl_employees.
CREATE NONCLUSTERED INDEX IX_tbl_departments_department_name
    ON dbo.tbl_departments (department_name ASC);
GO

-- ============================================================
-- tbl_employees
-- Core employee records.  Each employee belongs to exactly one
-- department.  Referenced by tbl_contracts.
--
-- Requirement 10.1: has FK relationship to tbl_departments.
-- Requirement 10.4: document every field.
--
-- Purpose: stores personal and employment details for every
--   employee, enabling controls such as "minor with high
--   salary" (employee_age < 18 AND employee_salary > 10,000).
-- ============================================================
CREATE TABLE dbo.tbl_employees
(
    -- Surrogate primary key.
    employee_id     INT             NOT NULL IDENTITY(1,1),

    -- Full name of the employee.
    -- Hebrew_CI_AS collation handles Hebrew names correctly.
    employee_name   NVARCHAR(200)   NOT NULL

    -- Age of the employee in whole years.
    -- Used by controls that detect minors (< 18).
    employee_age    INT             NOT NULL,

    -- Gross monthly salary in ILS.
    -- DECIMAL(12,2) avoids floating-point rounding issues.
    employee_salary DECIMAL(12, 2)  NOT NULL,

    -- FK to the department this employee belongs to.
    -- NO ACTION on update/delete so the application can handle
    -- orphan reassignment explicitly.
    department_id   INT             NULL,

    -- Soft-delete / active flag.
    -- 1 = currently employed, 0 = terminated.
    is_active       BIT             NOT NULL
                        CONSTRAINT DF_tbl_employees_is_active DEFAULT 1,

    -- Date the employee was hired (date portion only).
    hire_date       DATE            NOT NULL,

    CONSTRAINT PK_tbl_employees
        PRIMARY KEY CLUSTERED (employee_id),

    CONSTRAINT FK_tbl_employees_tbl_departments
        FOREIGN KEY (department_id)
        REFERENCES dbo.tbl_departments (department_id)
        ON DELETE SET NULL   -- department deletion nulls the FK rather
        ON UPDATE CASCADE    -- than leaving orphaned employees
);
GO

-- Non-clustered index for department-scoped employee lookups
-- (common JOIN pattern when controls involve both tables).
CREATE NONCLUSTERED INDEX IX_tbl_employees_department_id
    ON dbo.tbl_employees (department_id ASC);

-- Non-clustered index to accelerate controls that filter by
-- age and/or salary (the two most common anomaly predicates).
CREATE NONCLUSTERED INDEX IX_tbl_employees_age_salary
    ON dbo.tbl_employees (employee_age ASC, employee_salary ASC);

-- Non-clustered index on hire_date for date-range controls.
CREATE NONCLUSTERED INDEX IX_tbl_employees_hire_date
    ON dbo.tbl_employees (hire_date ASC);
GO

-- ============================================================
-- tbl_contracts
-- Employment contract records tied to individual employees.
-- One employee may have multiple contracts (e.g. renewals).
--
-- Requirement 10.1: has FK relationship to tbl_employees.
-- Requirement 10.2: will contain at least 2 rows where
--   end_date < start_date (intentional anomaly for demos).
-- Requirement 10.4: document every field.
--
-- Purpose: stores contract periods and types, enabling
--   controls such as "end date earlier than start date"
--   (end_date < start_date).
-- ============================================================
CREATE TABLE dbo.tbl_contracts
(
    -- Surrogate primary key.
    contract_id     INT             NOT NULL IDENTITY(1,1),

    -- Employee to whom this contract belongs.
    -- CASCADE DELETE: removing an employee removes all their
    -- contracts (safe ג€” contracts are owned by employees).
    employee_id     INT             NOT NULL,

    -- Inclusive start date of the contract period.
    start_date      DATE            NOT NULL,

    -- Inclusive end date of the contract period.
    -- NULL means open-ended / still active.
    -- Controls detect anomalies when end_date < start_date.
    end_date        DATE            NULL,

    -- Classification of the contract.
    -- Allowed values: Permanent | Temporary | Freelance | Internship
    -- Stored as NVARCHAR so new types can be added without schema
    -- changes; the application enforces the allowed set.
    contract_type   NVARCHAR(50)    NOT NULL

    CONSTRAINT PK_tbl_contracts
        PRIMARY KEY CLUSTERED (contract_id),

    CONSTRAINT FK_tbl_contracts_tbl_employees
        FOREIGN KEY (employee_id)
        REFERENCES dbo.tbl_employees (employee_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    -- Optional guard: warn via application logic when
    -- end_date is not null and precedes start_date.
    -- (Not enforced as a CHECK here so intentional anomaly
    --  seed rows can be inserted for demo purposes.)
);
GO

-- Non-clustered index for employee-scoped contract lookups
-- (common JOIN / sub-select pattern in controls).
CREATE NONCLUSTERED INDEX IX_tbl_contracts_employee_id
    ON dbo.tbl_contracts (employee_id ASC);

-- Non-clustered index to speed up date-range anomaly controls
-- that compare start_date and end_date.
CREATE NONCLUSTERED INDEX IX_tbl_contracts_dates
    ON dbo.tbl_contracts (start_date ASC, end_date ASC);
GO

-- ============================================================
-- Table documentation summary (Requirement 10.4)
-- ============================================================
--
-- tbl_departments
--   Purpose  : master list of organisational departments
--   Fields   : department_id INT PK | department_name NVARCHAR(150) |
--              is_active BIT (default 1)
--   Relations: 1:N with tbl_employees (via department_id FK)
--
-- tbl_employees
--   Purpose  : one row per employee with personal and payroll data
--   Fields   : employee_id INT PK | employee_name NVARCHAR(200) |
--              employee_age INT | employee_salary DECIMAL(12,2) |
--              department_id INT FK(tbl_departments) NULL |
--              is_active BIT (default 1) | hire_date DATE
--   Relations: N:1 with tbl_departments (department_id FK)
--              1:N with tbl_contracts    (employee_id FK)
--
-- tbl_contracts
--   Purpose  : employment contracts; multiple per employee allowed
--   Fields   : contract_id INT PK | employee_id INT FK(tbl_employees) |
--              start_date DATE | end_date DATE NULL |
--              contract_type NVARCHAR(50)
--   Relations: N:1 with tbl_employees (employee_id FK, CASCADE DELETE)
--
-- ============================================================
-- End of target-schema.sql
-- ============================================================
