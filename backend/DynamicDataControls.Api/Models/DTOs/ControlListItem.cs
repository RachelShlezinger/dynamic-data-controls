namespace DynamicDataControls.Api.Models.DTOs;

/// <summary>
/// Projected view of a control returned by the list endpoint.
/// Description is truncated to 100 characters server-side;
/// LastExecutedAt is null when the control has never been run.
/// Requirements: 1.5, 1.6
/// </summary>
public record ControlListItem(
    int      Id,
    string   Name,
    string?  Description,
    DateTime CreatedAt,
    DateTime? LastExecutedAt);
