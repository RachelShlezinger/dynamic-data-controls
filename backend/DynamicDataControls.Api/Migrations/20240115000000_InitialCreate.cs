using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DynamicDataControls.Api.Migrations;

/// <summary>
/// Manual EF Core migration that creates the full system-database schema.
/// Matches AppDbContext.OnModelCreating and system-schema.sql.
/// Requirements: 1.2, 4.3, 6.1, 6.2, 6.3, 6.4
/// </summary>
[Migration("20240115000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. Controls ──────────────────────────────────────────────────────────
        // No FK dependencies — create first.
        migrationBuilder.CreateTable(
            name: "Controls",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                    ,
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false,
                    defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false,
                    defaultValueSql: "GETUTCDATE()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Controls", x => x.Id);
            });

        // ── 2. BusinessEntities ──────────────────────────────────────────────────
        // No FK dependencies — create before BusinessFields.
        migrationBuilder.CreateTable(
            name: "BusinessEntities",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FriendlyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                    ,
                TableName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessEntities", x => x.Id);
            });

        // ── 3. BusinessFields ────────────────────────────────────────────────────
        // Depends on BusinessEntities (FK + CASCADE DELETE).
        migrationBuilder.CreateTable(
            name: "BusinessFields",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EntityId = table.Column<int>(type: "int", nullable: false),
                FriendlyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                    ,
                ColumnName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                DataType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessFields", x => x.Id);
                // Requirement 6.3: DataType restricted to exactly 4 allowed values.
                table.CheckConstraint(
                    "CK_BusinessFields_DataType",
                    "DataType IN ('Number', 'String', 'Date', 'Boolean')");
                table.ForeignKey(
                    name: "FK_BusinessFields_BusinessEntities",
                    column: x => x.EntityId,
                    principalTable: "BusinessEntities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ── 4. ControlExecutions ─────────────────────────────────────────────────
        // Depends on Controls (FK + CASCADE DELETE).
        migrationBuilder.CreateTable(
            name: "ControlExecutions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ControlId = table.Column<int>(type: "int", nullable: false),
                ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false,
                    defaultValueSql: "GETUTCDATE()"),
                AnomalyCount = table.Column<int>(type: "int", nullable: true),
                RunTimeMs = table.Column<int>(type: "int", nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ControlExecutions", x => x.Id);
                // Requirement 4.3: lifecycle states restricted to 4 allowed values.
                table.CheckConstraint(
                    "CK_ControlExecutions_Status",
                    "Status IN ('Running', 'Success', 'Error', 'Cancelled')");
                table.ForeignKey(
                    name: "FK_ControlExecutions_Controls_ControlId",
                    column: x => x.ControlId,
                    principalTable: "Controls",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ── 5. EntityRelationships ───────────────────────────────────────────────
        // Depends on BusinessEntities and BusinessFields.
        // All 4 FKs use NO ACTION to avoid multi-cascade-paths in SQL Server
        // (BusinessFields already cascades from BusinessEntities).
        migrationBuilder.CreateTable(
            name: "EntityRelationships",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SourceEntityId = table.Column<int>(type: "int", nullable: false),
                TargetEntityId = table.Column<int>(type: "int", nullable: false),
                SourceFieldId = table.Column<int>(type: "int", nullable: false),
                TargetFieldId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EntityRelationships", x => x.Id);
                table.ForeignKey(
                    name: "FK_EntityRelationships_SourceEntity",
                    column: x => x.SourceEntityId,
                    principalTable: "BusinessEntities",
                    principalColumn: "Id");   // NO ACTION
                table.ForeignKey(
                    name: "FK_EntityRelationships_TargetEntity",
                    column: x => x.TargetEntityId,
                    principalTable: "BusinessEntities",
                    principalColumn: "Id");   // NO ACTION
                table.ForeignKey(
                    name: "FK_EntityRelationships_SourceField",
                    column: x => x.SourceFieldId,
                    principalTable: "BusinessFields",
                    principalColumn: "Id");   // NO ACTION
                table.ForeignKey(
                    name: "FK_EntityRelationships_TargetField",
                    column: x => x.TargetFieldId,
                    principalTable: "BusinessFields",
                    principalColumn: "Id");   // NO ACTION
            });

        // ── Indexes ──────────────────────────────────────────────────────────────

        // Controls — unique case-insensitive name (Hebrew_CI_AS collation handles CI).
        // Requirement 1.8: duplicate name check must be case-insensitive.
        migrationBuilder.CreateIndex(
            name: "UQ_Controls_Name",
            table: "Controls",
            column: "Name",
            unique: true);

        // BusinessEntities — unique FriendlyName.
        // Requirement 6.8: duplicates rejected (case-insensitive).
        migrationBuilder.CreateIndex(
            name: "UQ_BusinessEntities_FriendlyName",
            table: "BusinessEntities",
            column: "FriendlyName",
            unique: true);

        // BusinessFields — unique (EntityId, FriendlyName) within each entity.
        // Requirement 6.2: field name unique per entity.
        migrationBuilder.CreateIndex(
            name: "UQ_BusinessFields_EntityId_FriendlyName",
            table: "BusinessFields",
            columns: new[] { "EntityId", "FriendlyName" },
            unique: true);

        // BusinessFields — optimise "all fields for entity X" queries.
        migrationBuilder.CreateIndex(
            name: "IX_BusinessFields_EntityId",
            table: "BusinessFields",
            column: "EntityId");

        // ControlExecutions — composite index for history queries and latest-run lookup.
        migrationBuilder.CreateIndex(
            name: "IX_ControlExecutions_ControlId_ExecutedAt",
            table: "ControlExecutions",
            columns: new[] { "ControlId", "ExecutedAt" });

        // EntityRelationships — composite index for "find relationship between A and B".
        migrationBuilder.CreateIndex(
            name: "IX_EntityRelationships_SourceTarget",
            table: "EntityRelationships",
            columns: new[] { "SourceEntityId", "TargetEntityId" });

        // Auto-indexes for FK columns not covered by the composite index above.
        migrationBuilder.CreateIndex(
            name: "IX_EntityRelationships_TargetEntityId",
            table: "EntityRelationships",
            column: "TargetEntityId");

        migrationBuilder.CreateIndex(
            name: "IX_EntityRelationships_SourceFieldId",
            table: "EntityRelationships",
            column: "SourceFieldId");

        migrationBuilder.CreateIndex(
            name: "IX_EntityRelationships_TargetFieldId",
            table: "EntityRelationships",
            column: "TargetFieldId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop in reverse dependency order so FK constraints do not block drops.
        migrationBuilder.DropTable(name: "EntityRelationships");
        migrationBuilder.DropTable(name: "ControlExecutions");
        migrationBuilder.DropTable(name: "BusinessFields");
        migrationBuilder.DropTable(name: "Controls");
        migrationBuilder.DropTable(name: "BusinessEntities");
    }
}
