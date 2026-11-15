-- ============================================================
-- Dynamic Data Controls — Demo Controls Seed Script
-- ============================================================
-- Requirements: 10.3, 10.5, 10.6
--
-- Prerequisites: Run the following scripts first:
--   1. system-schema.sql  → against DynamicDataControlsDB
--   2. seed-data.sql      → seeds BusinessEntities / BusinessFields /
--                           EntityRelationships that the DefinitionJson
--                           field names resolve against
--
-- This script inserts 4 demo controls into dbo.Controls.
-- The DefinitionJson values are identical to those seeded by
-- DatabaseInitializer.cs at application startup, so the SQL
-- script and the EF Core path stay in sync.
--
-- Guard: the entire INSERT block is skipped when dbo.Controls
-- already contains rows, making re-runs safe.
--
-- ── Controls inserted ──────────────────────────────────────
--   1. "עובדים קטינים עם שכר גבוה"
--        Entity  : עובד
--        Logic   : גיל < 18 AND שכר_חודשי > 10,000
--        Anomalies expected: 3 (Requirements 10.5)
--
--   2. "תאריך סיום מוקדם מתאריך התחלה"
--        Entity  : חוזה
--        Logic   : תאריך_סיום < {{תאריך_התחלה}}  (cross-field ref)
--        Anomalies expected: 2 (Requirements 10.6)
--
--   3. "עובדים פעילים במחלקה לא פעילה"
--        Entities: עובד JOIN מחלקה
--        Logic   : עובד.פעיל = true AND מחלקה.פעיל = false
--
--   4. "עובדים ללא שם"
--        Entity  : עובד
--        Logic   : שם_עובד IS NULL
-- ============================================================

-- USE DynamicDataControlsDB;
-- GO

IF NOT EXISTS (SELECT 1 FROM dbo.Controls)
BEGIN

    INSERT INTO dbo.Controls
        (Name, Description, DefinitionJson, CreatedAt, UpdatedAt)
    VALUES

    -- ── Control 1: עובדים קטינים עם שכר גבוה ──────────────────────────────────
    -- Detects employees under 18 with a gross monthly salary above 10,000 ₪.
    -- Single entity "עובד"; two conditions joined with AND.
    -- Requirement 10.5: seed data includes 3 intentional anomaly employees.
    (
        N'עובדים קטינים עם שכר גבוה',
        N'מאתרת עובדים מתחת לגיל 18 עם שכר מעל 10,000 ₪',
        N'{
  "primaryEntity": "עובד",
  "additionalEntities": [],
  "conditions": [
    {
      "id": "c1",
      "entityName": "עובד",
      "fieldName": "גיל",
      "operator": "LessThan",
      "value": 18,
      "logicalOperator": "AND"
    },
    {
      "id": "c2",
      "entityName": "עובד",
      "fieldName": "שכר_חודשי",
      "operator": "GreaterThan",
      "value": 10000,
      "logicalOperator": null
    }
  ]
}',
        GETUTCDATE(),
        GETUTCDATE()
    ),

    -- ── Control 2: תאריך סיום מוקדם מתאריך התחלה ───────────────────────────────
    -- Detects contracts where end_date is earlier than start_date.
    -- Value "{{תאריך_התחלה}}" is a cross-field reference template;
    -- QueryBuilderService resolves this to a column-vs-column comparison:
    --   WHERE e0.end_date < e0.start_date
    -- Requirement 10.6: seed data includes 2 intentional anomaly contracts.
    (
        N'תאריך סיום מוקדם מתאריך התחלה',
        N'מאתרת חוזים שבהם תאריך הסיום מוקדם מתאריך ההתחלה',
        N'{
  "primaryEntity": "חוזה",
  "additionalEntities": [],
  "conditions": [
    {
      "id": "c1",
      "entityName": "חוזה",
      "fieldName": "תאריך_סיום",
      "operator": "LessThan",
      "value": "{{תאריך_התחלה}}",
      "logicalOperator": null
    }
  ]
}',
        GETUTCDATE(),
        GETUTCDATE()
    ),

    -- ── Control 3: עובדים פעילים במחלקה לא פעילה ───────────────────────────────
    -- Detects active employees whose assigned department is marked inactive.
    -- Uses INNER JOIN between "עובד" and "מחלקה" via the seeded
    -- EntityRelationship (tbl_employees.department_id = tbl_departments.department_id).
    (
        N'עובדים פעילים במחלקה לא פעילה',
        N'מאתרת עובדים פעילים השייכים למחלקה שאינה פעילה',
        N'{
  "primaryEntity": "עובד",
  "additionalEntities": ["מחלקה"],
  "conditions": [
    {
      "id": "c1",
      "entityName": "עובד",
      "fieldName": "פעיל",
      "operator": "Equal",
      "value": true,
      "logicalOperator": "AND"
    },
    {
      "id": "c2",
      "entityName": "מחלקה",
      "fieldName": "פעיל",
      "operator": "Equal",
      "value": false,
      "logicalOperator": null
    }
  ]
}',
        GETUTCDATE(),
        GETUTCDATE()
    ),

    -- ── Control 4: עובדים ללא שם ────────────────────────────────────────────────
    -- Detects employees whose employee_name column is NULL.
    -- IsNull operator generates "col IS NULL" with no bound parameter.
    (
        N'עובדים ללא שם',
        N'מאתרת עובדים שאין להם שם רשום במסד הנתונים',
        N'{
  "primaryEntity": "עובד",
  "additionalEntities": [],
  "conditions": [
    {
      "id": "c1",
      "entityName": "עובד",
      "fieldName": "שם_עובד",
      "operator": "IsNull",
      "value": null,
      "logicalOperator": null
    }
  ]
}',
        GETUTCDATE(),
        GETUTCDATE()
    );

    PRINT N'4 demo controls inserted into dbo.Controls.';

END
ELSE
BEGIN
    PRINT N'dbo.Controls is not empty — demo controls seed skipped.';
END
GO

-- ============================================================
-- Verification query (run manually to confirm insert)
-- ============================================================
/*
SELECT
    Id,
    Name,
    Description,
    CreatedAt,
    LEN(DefinitionJson) AS DefinitionJsonLength
FROM dbo.Controls
ORDER BY Id;
*/

-- ============================================================
-- End of demo-controls.sql
-- ============================================================
