using DynamicDataControls.Api.Models.Entities;
using DynamicDataControls.Api.Services.Exceptions;

namespace DynamicDataControls.Api.Services.Interfaces;

/// <summary>
/// Business logic layer for metadata management (entities, fields, relationships).
/// All write operations enforce validation rules before delegating to the repository.
/// Requirements: 6.5, 6.6, 6.7, 6.8
/// </summary>
public interface IMetadataService
{
    // ── BusinessEntities ──────────────────────────────────────────────

    /// <summary>Returns all business entities ordered by FriendlyName.</summary>
    Task<IReadOnlyList<BusinessEntityModel>> GetAllEntitiesAsync();

    /// <summary>
    /// Returns the entity by id including its fields.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when no entity with <paramref name="id"/> exists.</exception>
    Task<BusinessEntityModel> GetEntityByIdAsync(int id);

    /// <summary>
    /// Creates a new business entity after validating:
    /// <list type="bullet">
    ///   <item><description><paramref name="friendlyName"/> is 1–100 chars (Req 6.1).</description></item>
    ///   <item><description>No existing entity shares the same name case-insensitively → <see cref="ConflictException"/> (Req 6.8).</description></item>
    ///   <item><description>The mapped table exists in the target DB → <see cref="ArgumentException"/> with table name (Req 6.5, 6.7).</description></item>
    /// </list>
    /// </summary>
    /// <exception cref="ArgumentException">Validation failure.</exception>
    /// <exception cref="ConflictException">Duplicate friendly name.</exception>
    Task<BusinessEntityModel> CreateEntityAsync(string friendlyName, string tableName, string? description);

    // ── BusinessFields ─────────────────────────────────────────────────

    /// <summary>Returns all fields for the given entity.</summary>
    /// <exception cref="NotFoundException">Thrown when the parent entity does not exist.</exception>
    Task<IReadOnlyList<BusinessField>> GetFieldsByEntityIdAsync(int entityId);

    /// <summary>
    /// Creates a new business field after validating:
    /// <list type="bullet">
    ///   <item><description>Parent entity exists.</description></item>
    ///   <item><description><paramref name="friendlyName"/> is 1–100 chars.</description></item>
    ///   <item><description><paramref name="dataType"/> is one of: Number, String, Date, Boolean.</description></item>
    ///   <item><description>No existing field in the entity shares the same <paramref name="friendlyName"/> case-insensitively → <see cref="ConflictException"/>.</description></item>
    /// </list>
    /// </summary>
    /// <exception cref="NotFoundException">Parent entity not found.</exception>
    /// <exception cref="ArgumentException">Validation failure.</exception>
    /// <exception cref="ConflictException">Duplicate friendly name within entity.</exception>
    Task<BusinessField> CreateFieldAsync(int entityId, string friendlyName, string columnName, string dataType, string? description);

    // ── EntityRelationships ────────────────────────────────────────────

    /// <summary>Returns all relationships with navigation properties populated.</summary>
    Task<IReadOnlyList<EntityRelationship>> GetAllRelationshipsAsync();

    /// <summary>
    /// Creates a new JOIN relationship after validating:
    /// <list type="bullet">
    ///   <item><description>Both source and target entities exist.</description></item>
    ///   <item><description>Source field belongs to the source entity (Req 6.6).</description></item>
    ///   <item><description>Target field belongs to the target entity (Req 6.6).</description></item>
    /// </list>
    /// </summary>
    /// <exception cref="NotFoundException">An entity or field was not found.</exception>
    /// <exception cref="ArgumentException">A field does not belong to its declared entity.</exception>
    Task<EntityRelationship> CreateRelationshipAsync(
        int sourceEntityId, int targetEntityId,
        int sourceFieldId,  int targetFieldId);

    // ── AI Context ────────────────────────────────────────────────────

    /// <summary>
    /// Returns all entities with their fields eagerly loaded.
    /// Used by <c>AiProxyService</c> to build the context payload sent to the Python service.
    /// </summary>
    Task<IReadOnlyList<BusinessEntityModel>> GetAllEntitiesWithFieldsAsync();
}
