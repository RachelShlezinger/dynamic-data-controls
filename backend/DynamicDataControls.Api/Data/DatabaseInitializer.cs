using DynamicDataControls.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DynamicDataControls.Api.Data;

/// <summary>
/// Applies pending EF Core migrations and seeds reference data at startup.
/// Call <see cref="InitializeAsync"/> from Program.cs before app.Run().
/// Requirements: 10.1, 10.3
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations, then seeds metadata and demo controls
    /// into empty tables.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger  = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DatabaseInitializer));

        try
        {
            logger.LogInformation("Ensuring database schema exists...");
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database schema ready.");

            // ── Seed business metadata if the table is empty ─────────────────────
            if (!await context.BusinessEntities.AnyAsync())
            {
                await SeedMetadataAsync(context, logger);
            }
            else
            {
                logger.LogDebug("BusinessEntities table already populated — skipping metadata seed.");
            }

            // ── Seed demo controls if the table is empty ─────────────────────────
            if (!await context.Controls.AnyAsync())
            {
                await SeedControlsAsync(context, logger);
            }
            else
            {
                logger.LogDebug("Controls table already populated — skipping controls seed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialisation failed.");
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Metadata seed
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds 3 BusinessEntities, their BusinessFields, and 2 EntityRelationships.
    /// Requirements: 10.1, 10.3
    /// </summary>
    private static async Task SeedMetadataAsync(AppDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding business metadata (entities, fields, relationships)...");

        // ── BusinessEntities ──────────────────────────────────────────────────────
        var entityEmployee   = new BusinessEntityModel { FriendlyName = "עובד",   TableName = "tbl_employees",   Description = "עובדי הארגון" };
        var entityDepartment = new BusinessEntityModel { FriendlyName = "מחלקה",  TableName = "tbl_departments", Description = "מחלקות הארגון" };
        var entityContract   = new BusinessEntityModel { FriendlyName = "חוזה",   TableName = "tbl_contracts",   Description = "חוזי עבודה של עובדים" };

        context.BusinessEntities.AddRange(entityEmployee, entityDepartment, entityContract);
        await context.SaveChangesAsync();

        // ── BusinessFields for עובד (tbl_employees) ──────────────────────────────
        var fieldsEmployee = new List<BusinessField>
        {
            new() { EntityId = entityEmployee.Id, FriendlyName = "מזהה_עובד",   ColumnName = "employee_id",     DataType = "Number",  Description = "מזהה ייחודי של עובד (מפתח ראשי)" },
            new() { EntityId = entityEmployee.Id, FriendlyName = "שם_עובד",     ColumnName = "employee_name",   DataType = "String",  Description = "שם מלא של העובד" },
            new() { EntityId = entityEmployee.Id, FriendlyName = "גיל",         ColumnName = "employee_age",    DataType = "Number",  Description = "גיל העובד בשנים" },
            new() { EntityId = entityEmployee.Id, FriendlyName = "שכר_חודשי",   ColumnName = "employee_salary", DataType = "Number",  Description = "שכר חודשי ברוטו בש״ח" },
            new() { EntityId = entityEmployee.Id, FriendlyName = "פעיל",        ColumnName = "is_active",       DataType = "Boolean", Description = "האם העובד פעיל כרגע" },
            new() { EntityId = entityEmployee.Id, FriendlyName = "תאריך_קבלה", ColumnName = "hire_date",       DataType = "Date",    Description = "תאריך תחילת העסקה" },
            new() { EntityId = entityEmployee.Id, FriendlyName = "מזהה_מחלקה", ColumnName = "department_id",   DataType = "Number",  Description = "מזהה המחלקה של העובד (מפתח זר)" },
        };

        // ── BusinessFields for מחלקה (tbl_departments) ───────────────────────────
        var fieldsDepartment = new List<BusinessField>
        {
            new() { EntityId = entityDepartment.Id, FriendlyName = "מזהה_מחלקה",  ColumnName = "department_id",   DataType = "Number",  Description = "מזהה ייחודי של מחלקה (מפתח ראשי)" },
            new() { EntityId = entityDepartment.Id, FriendlyName = "שם_מחלקה",    ColumnName = "department_name", DataType = "String",  Description = "שם המחלקה" },
            new() { EntityId = entityDepartment.Id, FriendlyName = "פעיל",         ColumnName = "is_active",       DataType = "Boolean", Description = "האם המחלקה פעילה" },
        };

        // ── BusinessFields for חוזה (tbl_contracts) ──────────────────────────────
        var fieldsContract = new List<BusinessField>
        {
            new() { EntityId = entityContract.Id, FriendlyName = "מזהה_עובד",    ColumnName = "employee_id",   DataType = "Number", Description = "מזהה העובד בחוזה (מפתח זר)" },
            new() { EntityId = entityContract.Id, FriendlyName = "תאריך_התחלה",  ColumnName = "start_date",    DataType = "Date",   Description = "תאריך תחילת החוזה" },
            new() { EntityId = entityContract.Id, FriendlyName = "תאריך_סיום",   ColumnName = "end_date",      DataType = "Date",   Description = "תאריך סיום החוזה (null = חוזה פתוח)" },
            new() { EntityId = entityContract.Id, FriendlyName = "סוג_חוזה",     ColumnName = "contract_type", DataType = "String", Description = "סוג החוזה: Permanent / Temporary / Freelance / Internship" },
        };

        context.BusinessFields.AddRange(fieldsEmployee);
        context.BusinessFields.AddRange(fieldsDepartment);
        context.BusinessFields.AddRange(fieldsContract);
        await context.SaveChangesAsync();

        // ── EntityRelationships ───────────────────────────────────────────────────
        // Relationship 1: עובד → מחלקה  (tbl_employees.department_id = tbl_departments.department_id)
        var empDeptId   = fieldsEmployee.First(f => f.FriendlyName == "מזהה_מחלקה");
        var deptDeptId  = fieldsDepartment.First(f => f.FriendlyName == "מזהה_מחלקה");

        // Relationship 2: עובד → חוזה  (tbl_employees.employee_id = tbl_contracts.employee_id)
        var empEmpId      = fieldsEmployee.First(f => f.FriendlyName == "מזהה_עובד");
        var contractEmpId = fieldsContract.First(f => f.FriendlyName == "מזהה_עובד");

        context.EntityRelationships.AddRange(
            new EntityRelationship
            {
                SourceEntityId = entityEmployee.Id,
                TargetEntityId = entityDepartment.Id,
                SourceFieldId  = empDeptId.Id,
                TargetFieldId  = deptDeptId.Id
            },
            new EntityRelationship
            {
                SourceEntityId = entityEmployee.Id,
                TargetEntityId = entityContract.Id,
                SourceFieldId  = empEmpId.Id,
                TargetFieldId  = contractEmpId.Id
            }
        );
        await context.SaveChangesAsync();

        var totalFields = fieldsEmployee.Count + fieldsDepartment.Count + fieldsContract.Count;
        logger.LogInformation(
            "Metadata seeded: 3 entities, {FieldCount} fields, 2 relationships.",
            totalFields);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Controls seed
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds 4 demo controls with valid DefinitionJson matching the ControlDefinition JSON schema.
    /// Requirements: 10.3
    /// </summary>
    private static async Task SeedControlsAsync(AppDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding 4 demo controls...");

        var now = DateTime.UtcNow;

        // ── Control 1: עובדים קטינים עם שכר גבוה ────────────────────────────────
        // Detects employees under 18 with salary above 10,000 ₪.
        // Uses single entity "עובד", conditions joined with AND.
        const string control1Json = """
            {
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
            }
            """;

        // ── Control 2: תאריך סיום מוקדם מתאריך התחלה ────────────────────────────
        // Detects contracts where end_date < start_date.
        // Value "{{תאריך_התחלה}}" is a cross-field reference template;
        // QueryBuilderService will resolve this to a column-vs-column comparison.
        const string control2Json = """
            {
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
            }
            """;

        // ── Control 3: עובדים פעילים במחלקה לא פעילה ─────────────────────────────
        // Detects active employees whose department is inactive.
        // Uses JOIN between "עובד" and "מחלקה" (via the seeded EntityRelationship).
        const string control3Json = """
            {
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
            }
            """;

        // ── Control 4: עובדים ללא שם ─────────────────────────────────────────────
        // Detects employees with a NULL employee_name.
        // IsNull operator requires no value parameter.
        const string control4Json = """
            {
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
            }
            """;

        context.Controls.AddRange(
            new ControlEntity
            {
                Name           = "עובדים קטינים עם שכר גבוה",
                Description    = "מאתרת עובדים מתחת לגיל 18 עם שכר מעל 10,000 ₪",
                DefinitionJson = control1Json,
                CreatedAt      = now,
                UpdatedAt      = now
            },
            new ControlEntity
            {
                Name           = "תאריך סיום מוקדם מתאריך התחלה",
                Description    = "מאתרת חוזים שבהם תאריך הסיום מוקדם מתאריך ההתחלה",
                DefinitionJson = control2Json,
                CreatedAt      = now,
                UpdatedAt      = now
            },
            new ControlEntity
            {
                Name           = "עובדים פעילים במחלקה לא פעילה",
                Description    = "מאתרת עובדים פעילים השייכים למחלקה שאינה פעילה",
                DefinitionJson = control3Json,
                CreatedAt      = now,
                UpdatedAt      = now
            },
            new ControlEntity
            {
                Name           = "עובדים ללא שם",
                Description    = "מאתרת עובדים שאין להם שם רשום במסד הנתונים",
                DefinitionJson = control4Json,
                CreatedAt      = now,
                UpdatedAt      = now
            }
        );

        await context.SaveChangesAsync();
        logger.LogInformation("4 demo controls seeded.");
    }
}
