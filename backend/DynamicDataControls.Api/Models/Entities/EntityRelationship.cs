namespace DynamicDataControls.Api.Models.Entities;

/// <summary>
/// EF Core entity for the EntityRelationships table.
/// Stores the JOIN definition between two BusinessEntities so
/// QueryBuilderService can produce multi-entity queries.
/// Requirements: 6.4
/// </summary>
public class EntityRelationship
{
    /// <summary>Identity surrogate key.</summary>
    public int Id { get; set; }

    /// <summary>FK to the "left" (driving) entity in the JOIN.</summary>
    public int SourceEntityId { get; set; }

    /// <summary>FK to the "right" (joined) entity in the JOIN.</summary>
    public int TargetEntityId { get; set; }

    /// <summary>FK to the column in the source entity used in the ON clause.</summary>
    public int SourceFieldId { get; set; }

    /// <summary>FK to the column in the target entity used in the ON clause.</summary>
    public int TargetFieldId { get; set; }

    // ── Navigation ────────────────────────────────────────────────────
    // All FKs use NO ACTION delete — the application removes relationships
    // before deleting an entity (cascade paths from BusinessFields would
    // otherwise create a cycle in SQL Server).
    public BusinessEntityModel SourceEntity { get; set; } = null!;
    public BusinessEntityModel TargetEntity { get; set; } = null!;
    public BusinessField SourceField { get; set; } = null!;
    public BusinessField TargetField { get; set; } = null!;
}
