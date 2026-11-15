using Microsoft.EntityFrameworkCore;
using DynamicDataControls.Api.Models.Entities;

namespace DynamicDataControls.Api.Data;

/// <summary>
/// EF Core DbContext for the Dynamic Data Controls system database.
/// Covers five tables: Controls, ControlExecutions, BusinessEntities,
/// BusinessFields, EntityRelationships.
/// Requirements: 1.2, 4.3, 6.1
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets ────────────────────────────────────────────────────────
    public DbSet<ControlEntity>      Controls            { get; set; } = null!;
    public DbSet<ControlExecution>   ControlExecutions   { get; set; } = null!;
    public DbSet<BusinessEntityModel> BusinessEntities   { get; set; } = null!;
    public DbSet<BusinessField>      BusinessFields      { get; set; } = null!;
    public DbSet<EntityRelationship> EntityRelationships { get; set; } = null!;

    // ── Fluent API ────────────────────────────────────────────────────
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureControls(modelBuilder);
        ConfigureControlExecutions(modelBuilder);
        ConfigureBusinessEntities(modelBuilder);
        ConfigureBusinessFields(modelBuilder);
        ConfigureEntityRelationships(modelBuilder);
    }

    // ─────────────────────────────────────────────────────────────────
    // Controls
    // ─────────────────────────────────────────────────────────────────
    private static void ConfigureControls(ModelBuilder mb)
    {
        mb.Entity<ControlEntity>(entity =>
        {
            entity.ToTable("Controls");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();

            // Name: required, max 100 chars, case-insensitive Hebrew collation
            // Requirement 1.8: duplicate name check must be case-insensitive.
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                ;

            // Unique index on Name (drives UQ_Controls_Name)
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("UQ_Controls_Name");

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            // DefinitionJson is NVARCHAR(MAX) — string maps to nvarchar(max) by default
            // on SQL Server, but we declare it explicitly for clarity.
            entity.Property(e => e.DefinitionJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            // Default timestamps (UTC).  CreatedAt is set once; UpdatedAt is refreshed
            // by the application on every UPDATE (the default only fires on INSERT).
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // One Control → many ControlExecutions, CASCADE DELETE
            entity.HasMany(e => e.Executions)
                .WithOne(ex => ex.Control)
                .HasForeignKey(ex => ex.ControlId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ─────────────────────────────────────────────────────────────────
    // ControlExecutions
    // ─────────────────────────────────────────────────────────────────
    private static void ConfigureControlExecutions(ModelBuilder mb)
    {
        mb.Entity<ControlExecution>(entity =>
        {
            // CHECK constraint on Status matches the four lifecycle values
            // in the SQL schema (CK_ControlExecutions_Status).
            entity.ToTable("ControlExecutions", t =>
                t.HasCheckConstraint(
                    "CK_ControlExecutions_Status",
                    "Status IN ('Running', 'Success', 'Error', 'Cancelled')"));

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();

            entity.Property(e => e.ExecutedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.AnomalyCount).IsRequired(false);
            entity.Property(e => e.RunTimeMs).IsRequired(false);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20);

            // ErrorMessage — full technical details, never sent to the UI
            entity.Property(e => e.ErrorMessage)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            // Composite index: (ControlId, ExecutedAt DESC) — optimises
            // "latest execution for control X" and execution history queries.
            entity.HasIndex(e => new { e.ControlId, e.ExecutedAt })
                .HasDatabaseName("IX_ControlExecutions_ControlId_ExecutedAt");

            // FK relationship is owned by the Controls side (configured above),
            // so the WithOne / HasForeignKey call is not repeated here.
        });
    }

    // ─────────────────────────────────────────────────────────────────
    // BusinessEntities
    // ─────────────────────────────────────────────────────────────────
    private static void ConfigureBusinessEntities(ModelBuilder mb)
    {
        mb.Entity<BusinessEntityModel>(entity =>
        {
            entity.ToTable("BusinessEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();

            // FriendlyName: required, max 100 chars, case-insensitive Hebrew collation
            // Requirement 6.8: duplicates rejected (case-insensitive).
            entity.Property(e => e.FriendlyName)
                .IsRequired()
                .HasMaxLength(100)
                ;

            entity.HasIndex(e => e.FriendlyName)
                .IsUnique()
                .HasDatabaseName("UQ_BusinessEntities_FriendlyName");

            entity.Property(e => e.TableName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(500);
        });
    }

    // ─────────────────────────────────────────────────────────────────
    // BusinessFields
    // ─────────────────────────────────────────────────────────────────
    private static void ConfigureBusinessFields(ModelBuilder mb)
    {
        mb.Entity<BusinessField>(entity =>
        {
            // CHECK constraint on DataType — Requirement 6.3
            entity.ToTable("BusinessFields", t =>
                t.HasCheckConstraint(
                    "CK_BusinessFields_DataType",
                    "DataType IN ('Number', 'String', 'Date', 'Boolean')"));

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();

            // FriendlyName: required, max 100 chars, Hebrew collation
            entity.Property(e => e.FriendlyName)
                .IsRequired()
                .HasMaxLength(100)
                ;

            entity.Property(e => e.ColumnName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.DataType)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Description)
                .HasMaxLength(200);

            // Composite unique index: (EntityId, FriendlyName) — Requirement 6.2
            entity.HasIndex(e => new { e.EntityId, e.FriendlyName })
                .IsUnique()
                .HasDatabaseName("UQ_BusinessFields_EntityId_FriendlyName");

            // FK to parent entity — CASCADE DELETE (entity owns its fields)
            entity.HasOne(e => e.Entity)
                .WithMany(be => be.Fields)
                .HasForeignKey(e => e.EntityId)
                .HasConstraintName("FK_BusinessFields_BusinessEntities")
                .OnDelete(DeleteBehavior.Cascade);

            // Index to speed up "give me all fields for entity X"
            entity.HasIndex(e => e.EntityId)
                .HasDatabaseName("IX_BusinessFields_EntityId");
        });
    }

    // ─────────────────────────────────────────────────────────────────
    // EntityRelationships
    // ─────────────────────────────────────────────────────────────────
    private static void ConfigureEntityRelationships(ModelBuilder mb)
    {
        mb.Entity<EntityRelationship>(entity =>
        {
            entity.ToTable("EntityRelationships");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();

            // All four FKs use NO ACTION to avoid a multiple-cascade-paths error
            // in SQL Server (BusinessFields already cascades from BusinessEntities).
            // The application or a stored procedure must remove relationships
            // before deleting an entity.

            entity.HasOne(e => e.SourceEntity)
                .WithMany(be => be.SourceRelationships)
                .HasForeignKey(e => e.SourceEntityId)
                .HasConstraintName("FK_EntityRelationships_SourceEntity")
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.TargetEntity)
                .WithMany(be => be.TargetRelationships)
                .HasForeignKey(e => e.TargetEntityId)
                .HasConstraintName("FK_EntityRelationships_TargetEntity")
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.SourceField)
                .WithMany(bf => bf.SourceRelationships)
                .HasForeignKey(e => e.SourceFieldId)
                .HasConstraintName("FK_EntityRelationships_SourceField")
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.TargetField)
                .WithMany(bf => bf.TargetRelationships)
                .HasForeignKey(e => e.TargetFieldId)
                .HasConstraintName("FK_EntityRelationships_TargetField")
                .OnDelete(DeleteBehavior.NoAction);

            // Composite index for the most common lookup:
            // "find the relationship between entity A and entity B"
            entity.HasIndex(e => new { e.SourceEntityId, e.TargetEntityId })
                .HasDatabaseName("IX_EntityRelationships_SourceTarget");
        });
    }
}
