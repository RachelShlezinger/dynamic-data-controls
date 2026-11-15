using DynamicDataControls.Api.Models.Domain;

namespace DynamicDataControls.Api.Models.DTOs;

/// <summary>
/// Request DTO for creating a new control.
/// Requirements: 1.1, 1.3
/// </summary>
/// <param name="Name">Control name — required, max 100 characters.</param>
/// <param name="Description">Optional free-text description — max 500 characters.</param>
/// <param name="Definition">Structured condition definition serialized from the Angular condition builder.</param>
public record CreateControlRequest(
    string            Name,
    string?           Description,
    ControlDefinition Definition);

/// <summary>
/// Request DTO for updating an existing control.
/// Requirements: 1.4, 1.7, 1.8, 1.9
/// </summary>
/// <param name="Name">Updated name — required, max 100 characters.</param>
/// <param name="Description">Updated optional description — max 500 characters.</param>
/// <param name="Definition">Updated condition definition.</param>
public record UpdateControlRequest(
    string            Name,
    string?           Description,
    ControlDefinition Definition);

/// <summary>
/// Response DTO returned by the single-control detail endpoint (GET /api/controls/{id}).
/// Includes the full deserialized definition and execution metadata.
/// Requirements: 1.4, 1.5
/// </summary>
/// <param name="Id">Surrogate primary key.</param>
/// <param name="Name">Control name.</param>
/// <param name="Description">Optional free-text description (full — not truncated).</param>
/// <param name="Definition">Deserialized condition definition ready for the Angular condition builder.</param>
/// <param name="CreatedAt">UTC timestamp set at INSERT.</param>
/// <param name="UpdatedAt">UTC timestamp refreshed at every UPDATE.</param>
/// <param name="LastExecutedAt">UTC timestamp of the most recent execution, or null if never run.</param>
public record ControlDetailResponse(
    int               Id,
    string            Name,
    string?           Description,
    ControlDefinition Definition,
    DateTime          CreatedAt,
    DateTime          UpdatedAt,
    DateTime?         LastExecutedAt);
