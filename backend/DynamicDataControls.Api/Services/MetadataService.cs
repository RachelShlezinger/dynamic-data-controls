using DynamicDataControls.Api.Models.Entities;
using DynamicDataControls.Api.Repositories.Interfaces;
using DynamicDataControls.Api.Services.Exceptions;
using DynamicDataControls.Api.Services.Interfaces;

namespace DynamicDataControls.Api.Services;

/// <summary>
/// Implements metadata management with business-rule validation.
/// Delegates persistence to <see cref="IMetadataRepository"/> only after
/// all validations pass.
/// Requirements: 6.5, 6.6, 6.7, 6.8
/// </summary>
public class MetadataService : IMetadataService
{
    private static readonly HashSet<string> ValidDataTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Number", "String", "Date", "Boolean" };

    private const int MaxFriendlyNameLength = 100;

    private readonly IMetadataRepository _repo;

    public MetadataService(IMetadataRepository repo)
    {
        _repo = repo;
    }

    // ── BusinessEntities ──────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<IReadOnlyList<BusinessEntityModel>> GetAllEntitiesAsync()
        => _repo.GetAllEntitiesAsync();

    /// <inheritdoc/>
    public async Task<BusinessEntityModel> GetEntityByIdAsync(int id)
    {
        var entity = await _repo.GetEntityByIdAsync(id);
        if (entity is null)
            throw new NotFoundException($"ישות עסקית עם מזהה {id} לא נמצאה.");
        return entity;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Validation order (Req 6.1, 6.5, 6.7, 6.8):
    /// 1. FriendlyName must be 1–100 chars.
    /// 2. FriendlyName must be unique (case-insensitive) → 409 ConflictException.
    /// 3. TableName must exist in the target DB → ArgumentException with table name.
    /// </remarks>
    public async Task<BusinessEntityModel> CreateEntityAsync(
        string friendlyName,
        string tableName,
        string? description)
    {
        // 1. FriendlyName length validation
        if (string.IsNullOrWhiteSpace(friendlyName))
            throw new ArgumentException("שם ידידותי הוא שדה חובה.", nameof(friendlyName));
        if (friendlyName.Length > MaxFriendlyNameLength)
            throw new ArgumentException(
                $"שם ידידותי לא יכול לעלות על {MaxFriendlyNameLength} תווים.",
                nameof(friendlyName));

        // 2. Case-insensitive name uniqueness (Req 6.8)
        var existing = await _repo.GetEntityByNameAsync(friendlyName);
        if (existing is not null)
            throw new ConflictException(
                $"ישות עסקית בשם \"{friendlyName}\" כבר קיימת במערכת.");

        // 3. Verify mapped table exists in target DB (Req 6.5, 6.7)
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("שם טבלה הוא שדה חובה.", nameof(tableName));

        var tableExists = await _repo.TableExistsInTargetDbAsync(tableName);
        if (!tableExists)
            throw new ArgumentException(
                $"הטבלה \"{tableName}\" אינה קיימת במסד הנתונים היעד.",
                nameof(tableName));

        var entity = new BusinessEntityModel
        {
            FriendlyName = friendlyName.Trim(),
            TableName    = tableName.Trim(),
            Description  = description
        };

        return await _repo.CreateEntityAsync(entity);
    }

    // ── BusinessFields ─────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessField>> GetFieldsByEntityIdAsync(int entityId)
    {
        // Confirm entity exists before fetching its fields
        var entity = await _repo.GetEntityByIdAsync(entityId);
        if (entity is null)
            throw new NotFoundException($"ישות עסקית עם מזהה {entityId} לא נמצאה.");

        return await _repo.GetFieldsByEntityIdAsync(entityId);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Validation order:
    /// 1. Parent entity exists.
    /// 2. FriendlyName 1–100 chars.
    /// 3. DataType must be one of: Number, String, Date, Boolean.
    /// 4. FriendlyName must be unique within the entity (case-insensitive) → 409.
    /// </remarks>
    public async Task<BusinessField> CreateFieldAsync(
        int entityId,
        string friendlyName,
        string columnName,
        string dataType,
        string? description)
    {
        // 1. Parent entity exists
        var entity = await _repo.GetEntityByIdAsync(entityId);
        if (entity is null)
            throw new NotFoundException($"ישות עסקית עם מזהה {entityId} לא נמצאה.");

        // 2. FriendlyName length validation
        if (string.IsNullOrWhiteSpace(friendlyName))
            throw new ArgumentException("שם ידידותי הוא שדה חובה.", nameof(friendlyName));
        if (friendlyName.Length > MaxFriendlyNameLength)
            throw new ArgumentException(
                $"שם ידידותי לא יכול לעלות על {MaxFriendlyNameLength} תווים.",
                nameof(friendlyName));

        // 3. DataType validation
        if (!ValidDataTypes.Contains(dataType))
            throw new ArgumentException(
                $"סוג נתונים \"{dataType}\" אינו חוקי. ערכים מותרים: Number, String, Date, Boolean.",
                nameof(dataType));

        // 4. FriendlyName uniqueness within entity (case-insensitive)
        var existingFields = await _repo.GetFieldsByEntityIdAsync(entityId);
        var duplicate = existingFields.FirstOrDefault(
            f => string.Equals(f.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase));

        if (duplicate is not null)
            throw new ConflictException(
                $"שדה בשם \"{friendlyName}\" כבר קיים בישות \"{entity.FriendlyName}\".");

        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("שם עמודה הוא שדה חובה.", nameof(columnName));

        var field = new BusinessField
        {
            EntityId     = entityId,
            FriendlyName = friendlyName.Trim(),
            ColumnName   = columnName.Trim(),
            DataType     = dataType,
            Description  = description
        };

        return await _repo.CreateFieldAsync(field);
    }

    // ── EntityRelationships ────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<IReadOnlyList<EntityRelationship>> GetAllRelationshipsAsync()
        => _repo.GetAllRelationshipsAsync();

    /// <inheritdoc/>
    /// <remarks>
    /// Validation order (Req 6.6):
    /// 1. Source entity exists.
    /// 2. Target entity exists.
    /// 3. Source field belongs to source entity.
    /// 4. Target field belongs to target entity.
    /// </remarks>
    public async Task<EntityRelationship> CreateRelationshipAsync(
        int sourceEntityId,
        int targetEntityId,
        int sourceFieldId,
        int targetFieldId)
    {
        // 1 & 2. Both entities must exist (GetEntityByIdAsync includes Fields)
        var sourceEntity = await _repo.GetEntityByIdAsync(sourceEntityId);
        if (sourceEntity is null)
            throw new NotFoundException(
                $"ישות מקור עם מזהה {sourceEntityId} לא נמצאה.");

        var targetEntity = await _repo.GetEntityByIdAsync(targetEntityId);
        if (targetEntity is null)
            throw new NotFoundException(
                $"ישות יעד עם מזהה {targetEntityId} לא נמצאה.");

        // 3. Source field belongs to source entity (Req 6.6)
        var sourceField = sourceEntity.Fields.FirstOrDefault(f => f.Id == sourceFieldId);
        if (sourceField is null)
            throw new ArgumentException(
                $"שדה עם מזהה {sourceFieldId} אינו שייך לישות \"{sourceEntity.FriendlyName}\".",
                nameof(sourceFieldId));

        // 4. Target field belongs to target entity (Req 6.6)
        var targetField = targetEntity.Fields.FirstOrDefault(f => f.Id == targetFieldId);
        if (targetField is null)
            throw new ArgumentException(
                $"שדה עם מזהה {targetFieldId} אינו שייך לישות \"{targetEntity.FriendlyName}\".",
                nameof(targetFieldId));

        var rel = new EntityRelationship
        {
            SourceEntityId = sourceEntityId,
            TargetEntityId = targetEntityId,
            SourceFieldId  = sourceFieldId,
            TargetFieldId  = targetFieldId
        };

        return await _repo.CreateRelationshipAsync(rel);
    }

    // ── AI Context ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<IReadOnlyList<BusinessEntityModel>> GetAllEntitiesWithFieldsAsync()
        => _repo.GetAllEntitiesWithFieldsAsync();
}
