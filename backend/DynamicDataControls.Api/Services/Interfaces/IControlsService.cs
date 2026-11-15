using DynamicDataControls.Api.Models.Domain;
using DynamicDataControls.Api.Models.DTOs;
using DynamicDataControls.Api.Models.Entities;

namespace DynamicDataControls.Api.Services.Interfaces;

/// <summary>
/// Contract for Controls business-logic operations.
/// Validation, uniqueness checks and JSON serialization happen here;
/// raw persistence is delegated to <see cref="Repositories.Interfaces.IControlsRepository"/>.
/// Requirements: 1.1, 1.3, 1.7, 1.8, 1.9
/// </summary>
public interface IControlsService
{
    /// <summary>
    /// Returns a projected list of all controls, optionally filtered by
    /// <paramref name="search"/> (case-insensitive substring match on Name and Description).
    /// </summary>
    Task<IReadOnlyList<ControlListItem>> GetAllAsync(string? search);

    /// <summary>
    /// Returns the control entity together with its deserialized <see cref="ControlDefinition"/>.
    /// </summary>
    /// <exception cref="Exceptions.NotFoundException">
    /// Thrown when no control with <paramref name="id"/> exists.
    /// </exception>
    Task<(ControlEntity entity, ControlDefinition definition)> GetByIdAsync(int id);

    /// <summary>
    /// Validates <paramref name="request"/> and persists a new control.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when Name is empty/too long, Description is too long, or the
    /// definition contains no conditions.
    /// </exception>
    /// <exception cref="Exceptions.ConflictException">
    /// Thrown when a control with the same name already exists (case-insensitive).
    /// </exception>
    Task<ControlEntity> CreateAsync(CreateControlRequest request);

    /// <summary>
    /// Validates <paramref name="request"/> and updates the existing control with
    /// <paramref name="id"/>.  Saving the same name as before is allowed
    /// (the uniqueness check excludes the control being updated).
    /// </summary>
    /// <exception cref="Exceptions.NotFoundException">
    /// Thrown when no control with <paramref name="id"/> exists.
    /// </exception>
    /// <exception cref="ArgumentException">Validation failures.</exception>
    /// <exception cref="Exceptions.ConflictException">Duplicate name on another control.</exception>
    Task<ControlEntity> UpdateAsync(int id, UpdateControlRequest request);

    /// <summary>
    /// Deletes the control with <paramref name="id"/>.
    /// </summary>
    /// <exception cref="Exceptions.NotFoundException">
    /// Thrown when no control with <paramref name="id"/> exists.
    /// </exception>
    Task DeleteAsync(int id);
}
