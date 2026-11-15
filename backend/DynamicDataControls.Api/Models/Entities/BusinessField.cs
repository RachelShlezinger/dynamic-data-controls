namespace DynamicDataControls.Api.Models.Entities;

/// <summary>
/// EF Core entity for the BusinessFields table.
/// Maps a user-friendly field name to the technical column name within a BusinessEntity.
/// Requirements: 6.2, 6.3
/// </summary>
public class BusinessField
{
    /// <summary>Identity surrogate key.</summary>
    public int Id { get; set; }

    /// <summary>FK to parent <see cref="BusinessEntityModel"/>.</summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Friendly display name shown in the condition builder (e.g. "גיל", "שכר_חודשי").
    /// Stored with Hebrew_CI_AS collation. Unique within an entity.
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>Actual column name in the target DB (e.g. "employee_age").</summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Data type that governs which operators the UI presents.
    /// Allowed values: Number | String | Date | Boolean
    /// Enforced by a CHECK constraint in the database.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Optional description of the field (up to 200 chars).</summary>
    public string? Description { get; set; }

    // ── Navigation ────────────────────────────────────────────────────
    public BusinessEntityModel Entity { get; set; } = null!;
    public ICollection<EntityRelationship> SourceRelationships { get; set; } = new List<EntityRelationship>();
    public ICollection<EntityRelationship> TargetRelationships { get; set; } = new List<EntityRelationship>();
}
