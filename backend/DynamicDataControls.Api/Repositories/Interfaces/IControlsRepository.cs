using DynamicDataControls.Api.Models.DTOs;
using DynamicDataControls.Api.Models.Entities;

namespace DynamicDataControls.Api.Repositories.Interfaces;

/// <summary>
/// Contract for Controls table persistence operations.
/// Requirements: 1.5, 1.6
/// </summary>
public interface IControlsRepository
{
    /// <summary>
    /// Returns a projected list of all controls, optionally filtered by
    /// <paramref name="search"/> (case-insensitive substring match on Name and Description).
    /// Each item carries the last <c>ControlExecution.ExecutedAt</c> value, or
    /// <c>null</c> when the control has never been run.
    /// Description is truncated to 100 characters.
    /// </summary>
    Task<IReadOnlyList<ControlListItem>> GetAllAsync(string? search);

    /// <summary>
    /// Returns the full <see cref="ControlEntity"/> (including <c>DefinitionJson</c>),
    /// or <c>null</c> if no control with <paramref name="id"/> exists.
    /// </summary>
    Task<ControlEntity?> GetByIdAsync(int id);

    /// <summary>Persists a new control and returns it with its generated <c>Id</c>.</summary>
    Task<ControlEntity> CreateAsync(ControlEntity control);

    /// <summary>
    /// Persists changes to an existing control.
    /// <c>UpdatedAt</c> is automatically set to <see cref="DateTime.UtcNow"/>.
    /// Returns the updated entity.
    /// </summary>
    Task<ControlEntity> UpdateAsync(ControlEntity control);

    /// <summary>
    /// Deletes the control with <paramref name="id"/>.
    /// Throws <see cref="KeyNotFoundException"/> if no matching control exists.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Returns <c>true</c> when a control with <paramref name="name"/> already exists
    /// (case-insensitive — the Name column uses Hebrew_CI_AS collation).
    /// Pass <paramref name="excludeId"/> to skip a specific control during update checks.
    /// </summary>
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
}
