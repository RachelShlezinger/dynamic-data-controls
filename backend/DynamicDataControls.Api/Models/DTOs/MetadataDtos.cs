namespace DynamicDataControls.Api.Models.DTOs;

// ── Request DTOs ──────────────────────────────────────────────────────────────

/// <summary>
/// Request body for POST /api/metadata/entities.
/// Requirements: 6.1, 6.5, 6.7, 6.8
/// </summary>
public sealed record CreateEntityRequest(
    string  FriendlyName,
    string  TableName,
    string? Description);

/// <summary>
/// Request body for POST /api/metadata/entities/{id}/fields.
/// Requirements: 6.2, 6.3
/// </summary>
public sealed record CreateFieldRequest(
    string  FriendlyName,
    string  ColumnName,
    string  DataType,
    string? Description);

/// <summary>
/// Request body for POST /api/metadata/relationships.
/// Requirements: 6.4, 6.6
/// </summary>
public sealed record CreateRelationshipRequest(
    int SourceEntityId,
    int TargetEntityId,
    int SourceFieldId,
    int TargetFieldId);

// ── Response DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Flat entity summary returned from GET /api/metadata/entities.
/// Does not include the Fields collection to avoid over-fetching.
/// </summary>
public sealed record EntityDto(
    int     Id,
    string  FriendlyName,
    string  TableName,
    string? Description);

/// <summary>
/// Field response returned from GET /api/metadata/entities/{id}/fields
/// and POST /api/metadata/entities/{id}/fields.
/// </summary>
public sealed record FieldDto(
    int     Id,
    int     EntityId,
    string  FriendlyName,
    string  ColumnName,
    string  DataType,
    string? Description);

/// <summary>
/// Relationship response returned from GET /api/metadata/relationships
/// and POST /api/metadata/relationships.
/// </summary>
public sealed record RelationshipDto(
    int Id,
    int SourceEntityId,
    int TargetEntityId,
    int SourceFieldId,
    int TargetFieldId);
