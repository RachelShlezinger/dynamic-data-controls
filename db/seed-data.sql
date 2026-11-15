-- ============================================================
-- Dynamic Data Controls — Seed Data Script
-- ============================================================
-- Requirements: 10.2, 10.3, 10.5, 10.6
--
-- Prerequisites: Run the following scripts first:
--   1. target-schema.sql  → against DynamicDataControlsTargetDB
--   2. system-schema.sql  → against DynamicDataControlsDB
--
-- ── Record counts ──────────────────────────────────────────
--   SECTION A: Target DB (DynamicDataControlsTargetDB)
--     tbl_departments :  10 rows  (8 active + 2 inactive)
--     tbl_employees   :  82 rows
--     tbl_contracts   :  35 rows
--     TOTAL target    : 127 rows  ✓ (Requirement 10.2: ≥ 100)
--
--   SECTION B: System DB (DynamicDataControlsDB)
--     BusinessEntities    :  3 rows
--     BusinessFields      : 15 rows
--     EntityRelationships :  2 rows
--
-- ── Intentional anomalies ──────────────────────────────────
--   • 3 employees where employee_age < 18 AND employee_salary > 10,000
--     (Requirement 10.5: control "עובדים קטינים עם שכר גבוה")
--   • 2 contracts where end_date < start_date
--     (Requirement 10.6: control "תאריך סיום מוקדם מתאריך התחלה")
-- ============================================================


-- ============================================================
-- SECTION A: TARGET DATABASE
-- Connect to: DynamicDataControlsTargetDB
-- ============================================================
-- USE DynamicDataControlsTargetDB;
-- GO

-- ── Optional: clear existing seed data before re-seeding ───
-- DELETE FROM dbo.tbl_contracts;
-- DELETE FROM dbo.tbl_employees;
-- DELETE FROM dbo.tbl_departments;
-- ── If IDENTITY columns need reset, uncomment DBCC lines ───
-- DBCC CHECKIDENT('dbo.tbl_contracts',   RESEED, 0);
-- DBCC CHECKIDENT('dbo.tbl_employees',   RESEED, 0);
-- DBCC CHECKIDENT('dbo.tbl_departments', RESEED, 0);
-- GO

-- ============================================================
-- tbl_departments — 10 rows
-- Rows 9–10 are INACTIVE (is_active = 0):
--   • Legal (dept 9)        — decommissioned
--   • Administration (dept 10) — decommissioned
-- These inactive departments are referenced by employees so
-- the "employee in inactive department" demo control returns
-- results when executed.
-- ============================================================
INSERT INTO dbo.tbl_departments (department_name, is_active)
VALUES
    -- ── Active departments (8 rows) ──────────────────────
    (N'מחלקת IT',           1),   --  1
    (N'כספים',              1),   --  2
    (N'משאבי אנוש',         1),   --  3
    (N'מכירות',             1),   --  4
    (N'שיווק',              1),   --  5
    (N'תפעול',              1),   --  6
    (N'מחקר ופיתוח',        1),   --  7
    (N'שירות לקוחות',       1),   --  8
    -- ── Inactive departments (2 rows) ────────────────────
    (N'משפטי',              0),   --  9  INACTIVE
    (N'מינהל',              0);   -- 10  INACTIVE
GO

-- ============================================================
-- tbl_employees — 82 rows
--
-- Group A (rows 1–3):   INTENTIONAL ANOMALIES
--   employee_age < 18 AND employee_salary > 10,000
--   → Required by Requirement 10.5
--
-- Group B (rows 4–8):   Employees in INACTIVE departments
--   department_id IN (9, 10)
--   → Needed for the "employee in inactive department" control
--
-- Group C (rows 9–13):  TERMINATED employees (is_active = 0)
--
-- Groups D–K (rows 14–82): Regular employees across all 8
--   active departments (age 18–65, salary 5,000–50,000)
-- ============================================================
INSERT INTO dbo.tbl_employees
    (employee_name, employee_age, employee_salary, department_id, is_active, hire_date)
VALUES

-- ── Group A: Anomaly employees (age < 18, salary > 10,000) ─
-- !! INTENTIONAL ANOMALY — Requirement 10.5 !!
(N'יוסי כהן',       16, 15000.00,  1, 1, '2024-09-01'),  --  1
(N'שרה לוי',        17, 12500.00,  2, 1, '2024-07-01'),  --  2
(N'דוד אברהם',      15, 18000.00,  4, 1, '2024-08-01'),  --  3

-- ── Group B: Employees in inactive departments ───────────
(N'רחל גרין',       35,  8000.00,  9, 1, '2018-03-01'),  --  4  dept Legal (inactive)
(N'מיכאל בן-דוד',   42, 12000.00,  9, 1, '2015-11-01'),  --  5  dept Legal (inactive)
(N'אסתר כץ',        28,  7500.00, 10, 1, '2019-06-01'),  --  6  dept Admin (inactive)
(N'יעקב שפירא',     55, 15000.00, 10, 1, '2016-02-01'),  --  7  dept Admin (inactive)
(N'נועה פרידמן',    31,  9000.00,  9, 1, '2017-09-01'),  --  8  dept Legal (inactive)

-- ── Group C: Terminated employees (is_active = 0) ────────
(N'אבי גולדברג',    45, 20000.00,  1, 0, '2016-05-01'),  --  9
(N'לילי מזרחי',     38, 16000.00,  3, 0, '2017-08-01'),  -- 10
(N'חיים ברקוביץ',   52, 22000.00,  5, 0, '2015-01-01'),  -- 11
(N'מרים אלון',      29, 11000.00,  6, 0, '2019-04-01'),  -- 12
(N'שמעון כרמי',     61, 25000.00,  7, 0, '2014-07-01'),  -- 13

-- ── Group D: IT department (dept 1) ──────────────────────
(N'ארז לוי',        28, 35000.00,  1, 1, '2019-03-01'),  -- 14
(N'תמר כהן',        32, 28000.00,  1, 1, '2020-07-01'),  -- 15
(N'נדב שמואלי',     25, 22000.00,  1, 1, '2022-01-01'),  -- 16
(N'יובל ברוך',      40, 42000.00,  1, 1, '2016-09-01'),  -- 17
(N'ענת גרוס',       35, 30000.00,  1, 1, '2018-05-01'),  -- 18
(N'רן אביב',        27, 26000.00,  1, 1, '2021-03-01'),  -- 19
(N'שיר דניאל',      30, 31000.00,  1, 1, '2020-11-01'),  -- 20
(N'אמיר כץ',        45, 40000.00,  1, 1, '2015-06-01'),  -- 21
(N'שי אדרי',        23, 18000.00,  1, 1, '2023-02-01'),  -- 22
(N'ניצן פלדמן',     32, 37000.00,  1, 1, '2019-08-01'),  -- 23

-- ── Group E: Finance department (dept 2) ─────────────────
(N'ליאת שרת',       38, 32000.00,  2, 1, '2017-11-01'),  -- 24
(N'בני אמיר',       50, 45000.00,  2, 1, '2014-03-01'),  -- 25
(N'חנה גלזר',       29, 24000.00,  2, 1, '2021-07-01'),  -- 26
(N'אריה פרל',       43, 38000.00,  2, 1, '2016-01-01'),  -- 27
(N'צפורה בן-אריה',  35, 27000.00,  2, 1, '2018-09-01'),  -- 28
(N'אלכסנדר רוס',    28, 20000.00,  2, 1, '2022-04-01'),  -- 29
(N'מרגלית שלום',    58, 30000.00,  2, 1, '2013-06-01'),  -- 30
(N'רמי עזרא',       44, 27000.00,  2, 1, '2017-02-01'),  -- 31

-- ── Group F: HR department (dept 3) ──────────────────────
(N'מיה כהן',        33, 22000.00,  3, 1, '2019-10-01'),  -- 32
(N'גיל ספיר',       45, 28000.00,  3, 1, '2016-04-01'),  -- 33
(N'טל ניסן',        26, 18000.00,  3, 1, '2022-07-01'),  -- 34
(N'רות אמיתי',      55, 32000.00,  3, 1, '2015-03-01'),  -- 35
(N'עמרי דוד',       31, 21000.00,  3, 1, '2020-05-01'),  -- 36
(N'נגה ברק',        28, 19000.00,  3, 1, '2021-09-01'),  -- 37
(N'בר מזרחי',       26, 23000.00,  3, 1, '2022-02-01'),  -- 38
(N'זהבה שפירו',     61, 20000.00,  3, 1, '2012-11-01'),  -- 39

-- ── Group G: Sales department (dept 4) ───────────────────
(N'איתן רם',        36, 35000.00,  4, 1, '2018-07-01'),  -- 40
(N'קרן גל',         27, 25000.00,  4, 1, '2021-02-01'),  -- 41
(N'אסף הלוי',       42, 38000.00,  4, 1, '2016-11-01'),  -- 42
(N'אורן גדות',      30, 28000.00,  4, 1, '2020-09-01'),  -- 43
(N'שלומית כרמל',    24, 20000.00,  4, 1, '2023-01-01'),  -- 44
(N'יגאל מנחם',      48, 42000.00,  4, 1, '2015-08-01'),  -- 45
(N'דנה פורת',       35, 30000.00,  4, 1, '2018-12-01'),  -- 46
(N'אדיר הרצוג',     35, 29000.00,  4, 1, '2019-05-01'),  -- 47
(N'טובה ריינר',     35, 24000.00,  4, 1, '2019-11-01'),  -- 48

-- ── Group H: Marketing department (dept 5) ───────────────
(N'מאיה שמיר',      29, 27000.00,  5, 1, '2020-03-01'),  -- 49
(N'עדי גפן',        34, 24000.00,  5, 1, '2019-01-01'),  -- 50
(N'רועי בן-שחר',    38, 32000.00,  5, 1, '2017-06-01'),  -- 51
(N'שני אבני',       25, 19000.00,  5, 1, '2022-10-01'),  -- 52
(N'נמרוד קליין',    44, 35000.00,  5, 1, '2016-07-01'),  -- 53
(N'אביגיל צור',     31, 22000.00,  5, 1, '2020-01-01'),  -- 54
(N'עינב כסלו',      30, 25000.00,  5, 1, '2021-05-01'),  -- 55

-- ── Group I: Operations department (dept 6) ──────────────
(N'דרור נגב',       47, 28000.00,  6, 1, '2015-10-01'),  -- 56
(N'רינה אשכנזי',    39, 24000.00,  6, 1, '2017-04-01'),  -- 57
(N'עמוס ריינהולד',  55, 35000.00,  6, 1, '2014-05-01'),  -- 58
(N'שגית מלכה',      28, 20000.00,  6, 1, '2021-08-01'),  -- 59
(N'יוחנן גרבר',     42, 30000.00,  6, 1, '2017-01-01'),  -- 60
(N'אורית פנחס',     35, 25000.00,  6, 1, '2019-07-01'),  -- 61
(N'יורם ביטון',     48, 33000.00,  6, 1, '2016-03-01'),  -- 62

-- ── Group J: R&D department (dept 7) ─────────────────────
(N'אלון ורד',       31, 40000.00,  7, 1, '2020-02-01'),  -- 63
(N'ליה שפי',        27, 33000.00,  7, 1, '2021-10-01'),  -- 64
(N'נוי קובי',       24, 27000.00,  7, 1, '2023-03-01'),  -- 65
(N'אמנון רז',       40, 48000.00,  7, 1, '2016-12-01'),  -- 66
(N'שמרית יהודה',    36, 42000.00,  7, 1, '2018-08-01'),  -- 67
(N'בועז כרם',       43, 45000.00,  7, 1, '2015-05-01'),  -- 68
(N'מכבי אורן',      29, 35000.00,  7, 1, '2020-06-01'),  -- 69
(N'אלה גפן',        32, 38000.00,  7, 1, '2019-02-01'),  -- 70
(N'הדר ינאי',       24, 28000.00,  7, 1, '2023-05-01'),  -- 71

-- ── Group K: Customer Service department (dept 8) ─────────
(N'זיו אבו',        22, 14000.00,  8, 1, '2023-06-01'),  -- 72
(N'תלמה דרור',      28, 16000.00,  8, 1, '2021-11-01'),  -- 73
(N'ניר שחם',        33, 18000.00,  8, 1, '2019-09-01'),  -- 74
(N'נועם הכהן',      25, 15000.00,  8, 1, '2022-08-01'),  -- 75
(N'ורד אמיר',       40, 20000.00,  8, 1, '2016-10-01'),  -- 76
(N'גדי שגיא',       37, 17000.00,  8, 1, '2018-04-01'),  -- 77
(N'ילנה פוקס',      30, 16000.00,  8, 1, '2020-12-01'),  -- 78
(N'ריקי בן-דוד',    45, 19000.00,  8, 1, '2016-08-01'),  -- 79
(N'מוטי גלעד',      52, 22000.00,  8, 1, '2014-09-01'),  -- 80
(N'חגית נתן',       29, 15000.00,  8, 1, '2021-04-01'),  -- 81
(N'אמי קסטיאל',     38, 21000.00,  8, 1, '2017-07-01');  -- 82
GO

-- ============================================================
-- tbl_contracts — 35 rows
--
-- Rows 1–2:  INTENTIONAL ANOMALIES — end_date < start_date
--   → Required by Requirement 10.6
--   Ref: control "תאריך סיום מוקדם מתאריך התחלה"
--
-- Rows 3–35: Valid contracts (end_date > start_date, or NULL
--   for open-ended Permanent contracts)
--
-- Contract type mix: Permanent, Temporary, Freelance, Internship
-- ============================================================
INSERT INTO dbo.tbl_contracts (employee_id, start_date, end_date, contract_type)
VALUES

-- ── ANOMALY contracts (end_date < start_date) ─────────────
-- !! INTENTIONAL ANOMALY — Requirement 10.6 !!
(14, '2023-01-01', '2022-12-31', 'Temporary'),   --  1 !! end precedes start
(24, '2023-06-01', '2023-05-01', 'Freelance'),   --  2 !! end precedes start

-- ── Anomaly employees — Internship contracts ──────────────
( 1, '2024-09-01', '2025-02-28', 'Internship'),  --  3
( 2, '2024-07-01', '2025-01-31', 'Internship'),  --  4
( 3, '2024-08-01', '2025-01-31', 'Internship'),  --  5

-- ── Inactive-department employees — Permanent / Temporary ─
( 4, '2018-03-01',  NULL,        'Permanent'),   --  6
( 5, '2015-11-01',  NULL,        'Permanent'),   --  7
( 6, '2019-06-01', '2022-05-31', 'Temporary'),   --  8
( 7, '2016-02-01',  NULL,        'Permanent'),   --  9
( 8, '2017-09-01', '2020-08-31', 'Temporary'),   -- 10

-- ── IT department employees ───────────────────────────────
(15, '2020-07-01',  NULL,        'Permanent'),   -- 11
(16, '2022-01-01', '2024-12-31', 'Temporary'),   -- 12
(17, '2016-09-01',  NULL,        'Permanent'),   -- 13
(18, '2018-05-01', '2020-04-30', 'Freelance'),   -- 14
(19, '2021-03-01', '2024-02-29', 'Temporary'),   -- 15
(20, '2020-11-01',  NULL,        'Permanent'),   -- 16
(21, '2015-06-01',  NULL,        'Permanent'),   -- 17

-- ── Finance department employees ──────────────────────────
(25, '2014-03-01',  NULL,        'Permanent'),   -- 18
(26, '2021-07-01', '2024-06-30', 'Temporary'),   -- 19
(27, '2016-01-01',  NULL,        'Permanent'),   -- 20
(28, '2018-09-01', '2022-08-31', 'Freelance'),   -- 21
(29, '2022-04-01', '2022-09-30', 'Internship'),  -- 22

-- ── HR department employees ───────────────────────────────
(33, '2016-04-01',  NULL,        'Permanent'),   -- 23
(34, '2022-07-01', '2023-01-31', 'Internship'),  -- 24
(35, '2015-03-01',  NULL,        'Permanent'),   -- 25

-- ── Sales department employees ────────────────────────────
(40, '2018-07-01',  NULL,        'Permanent'),   -- 26
(42, '2016-11-01',  NULL,        'Permanent'),   -- 27
(45, '2015-08-01',  NULL,        'Permanent'),   -- 28

-- ── Marketing department employees ────────────────────────
(50, '2019-01-01', '2022-12-31', 'Freelance'),   -- 29
(53, '2016-07-01',  NULL,        'Permanent'),   -- 30

-- ── Operations department employees ───────────────────────
(60, '2017-01-01',  NULL,        'Permanent'),   -- 31

-- ── R&D department employees ──────────────────────────────
(63, '2020-02-01',  NULL,        'Permanent'),   -- 32
(66, '2016-12-01',  NULL,        'Permanent'),   -- 33
(68, '2015-05-01',  NULL,        'Permanent'),   -- 34

-- ── Customer Service employees ────────────────────────────
(76, '2016-10-01',  NULL,        'Permanent');   -- 35
GO


-- ============================================================
-- SECTION B: SYSTEM DATABASE
-- Connect to: DynamicDataControlsDB
-- ============================================================
-- USE DynamicDataControlsDB;
-- GO

-- ── Optional: clear existing metadata before re-seeding ────
-- DELETE FROM dbo.EntityRelationships;
-- DELETE FROM dbo.BusinessFields;
-- DELETE FROM dbo.BusinessEntities;
-- DBCC CHECKIDENT('dbo.EntityRelationships', RESEED, 0);
-- DBCC CHECKIDENT('dbo.BusinessFields',      RESEED, 0);
-- DBCC CHECKIDENT('dbo.BusinessEntities',    RESEED, 0);
-- GO

-- ============================================================
-- BusinessEntities — 3 rows
-- Maps Hebrew friendly entity names to target DB table names.
-- ============================================================
INSERT INTO dbo.BusinessEntities (FriendlyName, TableName, Description)
VALUES
    (N'עובד',   'tbl_employees',   N'ישות עובדים — נתוני עובדים פעילים ומסיימים'),
    (N'מחלקה',  'tbl_departments', N'ישות מחלקות — מחלקות ארגוניות פעילות ולא פעילות'),
    (N'חוזה',   'tbl_contracts',   N'ישות חוזים — חוזי העסקה המשויכים לעובדים');
GO

-- ============================================================
-- BusinessFields — 15 rows
-- Maps Hebrew friendly field names to technical column names.
-- DataType values: Number | String | Date | Boolean
--
-- ID assignments (predictable with IDENTITY starting at 1):
--   עובד  fields: 1–7
--   מחלקה fields: 8–10
--   חוזה  fields: 11–15
-- ============================================================

-- ── Fields for entity "עובד" (tbl_employees) ──────────────
INSERT INTO dbo.BusinessFields (EntityId, FriendlyName, ColumnName, DataType, Description)
SELECT
    e.Id,
    f.FriendlyName,
    f.ColumnName,
    f.DataType,
    f.Description
FROM dbo.BusinessEntities e
CROSS JOIN (VALUES
    (N'מזהה_עובד',    'employee_id',     'Number',  N'מזהה ייחודי של העובד'),
    (N'שם_עובד',      'employee_name',   'String',  N'שם מלא של העובד'),
    (N'גיל',          'employee_age',    'Number',  N'גיל העובד בשנים שלמות'),
    (N'שכר_חודשי',    'employee_salary', 'Number',  N'שכר חודשי ברוטו בש"ח'),
    (N'מחלקה_מזהה',   'department_id',   'Number',  N'מזהה המחלקה אליה משויך העובד'),
    (N'פעיל',         'is_active',       'Boolean', N'האם העובד מועסק כרגע'),
    (N'תאריך_קליטה',  'hire_date',       'Date',    N'תאריך קליטת העובד לארגון')
) AS f (FriendlyName, ColumnName, DataType, Description)
WHERE e.FriendlyName = N'עובד';
GO

-- ── Fields for entity "מחלקה" (tbl_departments) ───────────
INSERT INTO dbo.BusinessFields (EntityId, FriendlyName, ColumnName, DataType, Description)
SELECT
    e.Id,
    f.FriendlyName,
    f.ColumnName,
    f.DataType,
    f.Description
FROM dbo.BusinessEntities e
CROSS JOIN (VALUES
    (N'מזהה_מחלקה',  'department_id',   'Number',  N'מזהה ייחודי של המחלקה'),
    (N'שם_מחלקה',    'department_name', 'String',  N'שם תצוגה של המחלקה'),
    (N'פעיל',        'is_active',       'Boolean', N'האם המחלקה פעילה בארגון')
) AS f (FriendlyName, ColumnName, DataType, Description)
WHERE e.FriendlyName = N'מחלקה';
GO

-- ── Fields for entity "חוזה" (tbl_contracts) ──────────────
INSERT INTO dbo.BusinessFields (EntityId, FriendlyName, ColumnName, DataType, Description)
SELECT
    e.Id,
    f.FriendlyName,
    f.ColumnName,
    f.DataType,
    f.Description
FROM dbo.BusinessEntities e
CROSS JOIN (VALUES
    (N'מזהה_חוזה',      'contract_id',   'Number', N'מזהה ייחודי של החוזה'),
    (N'מזהה_עובד',      'employee_id',   'Number', N'מזהה העובד לו שייך החוזה'),
    (N'תאריך_התחלה',    'start_date',    'Date',   N'תאריך תחילת החוזה (כולל)'),
    (N'תאריך_סיום',     'end_date',      'Date',   N'תאריך סיום החוזה (כולל) — NULL עבור חוזים פתוחים'),
    (N'סוג_חוזה',       'contract_type', 'String', N'סיווג החוזה: Permanent / Temporary / Freelance / Internship')
) AS f (FriendlyName, ColumnName, DataType, Description)
WHERE e.FriendlyName = N'חוזה';
GO

-- ============================================================
-- EntityRelationships — 2 rows
--
-- Relationship 1: עובד ↔ מחלקה
--   JOIN: tbl_employees.department_id = tbl_departments.department_id
--   Enables controls that combine employee and department data,
--   e.g. "employee in an inactive department".
--
-- Relationship 2: עובד ↔ חוזה
--   JOIN: tbl_employees.employee_id = tbl_contracts.employee_id
--   Enables controls that combine employee and contract data,
--   e.g. "contract end date before start date".
--
-- SourceFieldId / TargetFieldId are resolved via sub-select
-- so this block is robust to IDENTITY restarts.
-- ============================================================
INSERT INTO dbo.EntityRelationships
    (SourceEntityId, TargetEntityId, SourceFieldId, TargetFieldId)

-- ── Relationship 1: עובד.מחלקה_מזהה → מחלקה.מזהה_מחלקה ──
SELECT
    emp.Id        AS SourceEntityId,
    dept.Id       AS TargetEntityId,
    srcF.Id       AS SourceFieldId,
    tgtF.Id       AS TargetFieldId
FROM   dbo.BusinessEntities  emp
JOIN   dbo.BusinessEntities  dept ON dept.FriendlyName = N'מחלקה'
JOIN   dbo.BusinessFields    srcF ON srcF.EntityId = emp.Id
                                 AND srcF.FriendlyName = N'מחלקה_מזהה'
JOIN   dbo.BusinessFields    tgtF ON tgtF.EntityId = dept.Id
                                 AND tgtF.FriendlyName = N'מזהה_מחלקה'
WHERE  emp.FriendlyName = N'עובד'

UNION ALL

-- ── Relationship 2: עובד.מזהה_עובד → חוזה.מזהה_עובד ──────
SELECT
    emp.Id        AS SourceEntityId,
    cont.Id       AS TargetEntityId,
    srcF.Id       AS SourceFieldId,
    tgtF.Id       AS TargetFieldId
FROM   dbo.BusinessEntities  emp
JOIN   dbo.BusinessEntities  cont ON cont.FriendlyName = N'חוזה'
JOIN   dbo.BusinessFields    srcF ON srcF.EntityId = emp.Id
                                 AND srcF.FriendlyName = N'מזהה_עובד'
JOIN   dbo.BusinessFields    tgtF ON tgtF.EntityId = cont.Id
                                 AND tgtF.FriendlyName = N'מזהה_עובד'
WHERE  emp.FriendlyName = N'עובד';
GO

-- ============================================================
-- Verification queries (run manually to confirm seed)
-- ============================================================
/*
-- Target DB totals (expect 10, 82, 35):
SELECT 'tbl_departments' AS tbl, COUNT(*) AS cnt FROM dbo.tbl_departments
UNION ALL
SELECT 'tbl_employees',         COUNT(*)          FROM dbo.tbl_employees
UNION ALL
SELECT 'tbl_contracts',         COUNT(*)          FROM dbo.tbl_contracts;

-- Requirement 10.5 — expect exactly 3:
SELECT employee_id, employee_name, employee_age, employee_salary
FROM   dbo.tbl_employees
WHERE  employee_age < 18
  AND  employee_salary > 10000;

-- Requirement 10.6 — expect exactly 2:
SELECT contract_id, employee_id, start_date, end_date
FROM   dbo.tbl_contracts
WHERE  end_date IS NOT NULL
  AND  end_date < start_date;

-- Inactive-department employees — expect 5:
SELECT e.employee_name, d.department_name, d.is_active
FROM   dbo.tbl_employees   e
JOIN   dbo.tbl_departments d ON d.department_id = e.department_id
WHERE  d.is_active = 0;

-- System DB — BusinessEntities (expect 3):
SELECT Id, FriendlyName, TableName FROM DynamicDataControlsDB.dbo.BusinessEntities;

-- System DB — BusinessFields per entity (expect 7, 3, 5):
SELECT be.FriendlyName AS Entity, COUNT(*) AS FieldCount
FROM   DynamicDataControlsDB.dbo.BusinessFields bf
JOIN   DynamicDataControlsDB.dbo.BusinessEntities be ON be.Id = bf.EntityId
GROUP BY be.FriendlyName;

-- System DB — EntityRelationships (expect 2):
SELECT
    src.FriendlyName  AS SourceEntity,
    tgt.FriendlyName  AS TargetEntity,
    sf.FriendlyName   AS SourceField,
    tf.FriendlyName   AS TargetField
FROM   DynamicDataControlsDB.dbo.EntityRelationships er
JOIN   DynamicDataControlsDB.dbo.BusinessEntities src ON src.Id = er.SourceEntityId
JOIN   DynamicDataControlsDB.dbo.BusinessEntities tgt ON tgt.Id = er.TargetEntityId
JOIN   DynamicDataControlsDB.dbo.BusinessFields   sf  ON sf.Id  = er.SourceFieldId
JOIN   DynamicDataControlsDB.dbo.BusinessFields   tf  ON tf.Id  = er.TargetFieldId;
*/

-- ============================================================
-- End of seed-data.sql
-- ============================================================
