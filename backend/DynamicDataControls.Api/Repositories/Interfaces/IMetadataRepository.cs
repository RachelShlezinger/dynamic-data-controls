using DynamicDataControls.Api.Models.Entities;

namespace DynamicDataControls.Api.Repositories.Interfaces;

/// <summary>
/// Contract for metadata persistence operations covering BusinessEntities,
/// BusinessFields, EntityRelationships, and target-DB introspection.
/// Requirements: 6.5, 6.7
/// </summary>
public interface IMetadataRepository
{
    // ── BusinessEntities ──────────────────────────────────────────────

    /// <summary>Returns all business entities ordered by FriendlyName.</summary>
    Task<IReadOnlyList<BusinessEntityModel>> GetAllEntitiesAsync();

    /// <summary>Returns the entity with its fields included, or null if not found.</summary>
    Task<BusinessEntityModel?> GetEntityByIdAsync(int id);

    /// <summary>
    /// Returns the entity matching <paramref name="friendlyName"/> (case-insensitive
    /// because the column uses Hebrew_CI_AS collation), or null if not found.
    /// </summary>
    Task<BusinessEntityModel?> GetEntityByNameAsync(string friendlyName);

    /// <summary>Persists a new entity and returns it with its generated Id.</summary>
    Task<BusinessEntityModel> CreateEntityAsync(BusinessEntityModel entity);

    // ── BusinessFields ─────────────────────────────────────────────────

    /// <summary>Returns all fields that belong to <paramref name="entityId"/>.</summary>
    Task<IReadOnlyList<BusinessField>> GetFieldsByEntityIdAsync(int entityId);

    /// <summary>Persists a new field and returns it with its generated Id.</summary>
    Task<BusinessField> CreateFieldAsync(BusinessField field);

    // ── EntityRelationships ────────────────────────────────────────────

    /// <summary>Returns all relationships with navigation properties populated.</summary>
    Task<IReadOnlyList<EntityRelationship>> GetAllRelationshipsAsync();

    /// <summary>Persists a new relationship and returns it with its generated Id.</summary>
    Task<EntityRelationship> CreateRelationshipAsync(EntityRelationship rel);

    /// <summary>
    /// Returns all entities with their fields eagerly loaded.
    /// Used by the AI proxy to build the context payload.
    /// </summary>
    Task<IReadOnlyList<BusinessEntityModel>> GetAllEntitiesWithFieldsAsync();

    // ── Target DB Introspection ────────────────────────────────────────

    /// <summary>
    /// Returns true when <paramref name="tableName"/> exists in the target database.
    /// Uses a parameterized query against INFORMATION_SCHEMA.TABLES — no string concatenation.
    /// Requirement 6.7
    /// </summary>
    Task<bool> TableExistsInTargetDbAsync(string tableName);

    /// <summary>
    /// Returns true when <paramref name="columnName"/> exists in <paramref name="tableName"/>
    /// in the target database.
    /// Uses a parameterized query against INFORMATION_SCHEMA.COLUMNS — no string concatenation.
    /// Requirement 6.7
    /// </summary>
    Task<bool> ColumnExistsInTargetDbAsync(string tableName, string columnName);
}
