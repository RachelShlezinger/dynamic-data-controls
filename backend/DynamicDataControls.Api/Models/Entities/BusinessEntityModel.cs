namespace DynamicDataControls.Api.Models.Entities;

/// <summary>
/// EF Core entity for the BusinessEntities table.
/// Maps a user-friendly business name to the technical table name in the target DB.
/// Requirements: 6.1
/// </summary>
public class BusinessEntityModel
{
    /// <summary>Identity surrogate key.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Friendly display name shown in the UI (e.g. "עובד").
    /// Stored with Hebrew_CI_AS collation — uniqueness is enforced case-insensitively.
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>Actual table name in the target DB (e.g. "tbl_employees").</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Optional description of the business entity.</summary>
    public string? Description { get; set; }

    // ── Navigation ────────────────────────────────────────────────────
    public ICollection<BusinessField> Fields { get; set; } = new List<BusinessField>();
    public ICollection<EntityRelationship> SourceRelationships { get; set; } = new List<EntityRelationship>();
    public ICollection<EntityRelationship> TargetRelationships { get; set; } = new List<EntityRelationship>();
}
