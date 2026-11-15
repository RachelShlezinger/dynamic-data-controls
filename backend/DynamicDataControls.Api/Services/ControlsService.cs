using System.Text.Json;
using DynamicDataControls.Api.Models.Domain;
using DynamicDataControls.Api.Models.DTOs;
using DynamicDataControls.Api.Models.Entities;
using DynamicDataControls.Api.Repositories.Interfaces;
using DynamicDataControls.Api.Services.Exceptions;
using DynamicDataControls.Api.Services.Interfaces;

namespace DynamicDataControls.Api.Services;

/// <summary>
/// Business-logic layer for Controls CRUD.
/// <para>
/// Validation is performed here — before any repository call — so the data layer
/// can remain simple and free of business rules.  The repository only receives
/// already-valid data.
/// </para>
/// <para>
/// <c>ControlDefinition</c> is stored as JSON in <c>Controls.DefinitionJson</c>;
/// serialization and deserialization use <see cref="JsonSerializerOptionsFactory.Create()"/>
/// to guarantee a correct round-trip (enum strings, camelCase, null omission).
/// </para>
/// Requirements: 1.1, 1.3, 1.7, 1.8, 1.9
/// </summary>
public class ControlsService : IControlsService
{
    private const int MaxNameLength        = 100;
    private const int MaxDescriptionLength = 500;

    // Cached once per process — JsonSerializerOptions is thread-safe after construction.
    private static readonly JsonSerializerOptions _jsonOptions =
        JsonSerializerOptionsFactory.Create();

    private readonly IControlsRepository _repo;

    public ControlsService(IControlsRepository repo) => _repo = repo;

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<IReadOnlyList<ControlListItem>> GetAllAsync(string? search)
        => _repo.GetAllAsync(search);

    /// <inheritdoc/>
    public async Task<(ControlEntity entity, ControlDefinition definition)> GetByIdAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            throw new NotFoundException($"בקרה עם מזהה {id} לא נמצאה.");

        var definition = Deserialize(entity.DefinitionJson, id);
        return (entity, definition);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ControlEntity> CreateAsync(CreateControlRequest request)
    {
        Validate(request.Name, request.Description, request.Definition);

        // Req 1.8: Case-insensitive name uniqueness (DB collation enforces CI_AS; EF == is enough)
        if (await _repo.NameExistsAsync(request.Name))
            throw new ConflictException(
                $"בקרה בשם \"{request.Name}\" כבר קיימת במערכת.");

        var now = DateTime.UtcNow;
        var entity = new ControlEntity
        {
            Name           = request.Name.Trim(),
            Description    = request.Description?.Trim(),
            DefinitionJson = JsonSerializer.Serialize(request.Definition, _jsonOptions),
            CreatedAt      = now,
            UpdatedAt      = now,
        };

        return await _repo.CreateAsync(entity);
    }

    /// <inheritdoc/>
    public async Task<ControlEntity> UpdateAsync(int id, UpdateControlRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            throw new NotFoundException($"בקרה עם מזהה {id} לא נמצאה.");

        Validate(request.Name, request.Description, request.Definition);

        // Req 1.8: Uniqueness check — exclude the current record so an unchanged name passes
        if (await _repo.NameExistsAsync(request.Name, excludeId: id))
            throw new ConflictException(
                $"בקרה בשם \"{request.Name}\" כבר קיימת במערכת.");

        existing.Name           = request.Name.Trim();
        existing.Description    = request.Description?.Trim();
        existing.DefinitionJson = JsonSerializer.Serialize(request.Definition, _jsonOptions);
        // UpdatedAt is refreshed automatically inside IControlsRepository.UpdateAsync.

        return await _repo.UpdateAsync(existing);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        // The repository's DeleteAsync throws KeyNotFoundException for missing records.
        // Wrap it in NotFoundException so the middleware returns a consistent 404.
        try
        {
            await _repo.DeleteAsync(id);
        }
        catch (KeyNotFoundException)
        {
            throw new NotFoundException($"בקרה עם מזהה {id} לא נמצאה.");
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Validates Name, Description, and Definition.Conditions according to
    /// requirements 1.1, 1.3, 1.7, 1.9.
    /// Throws <see cref="ArgumentException"/> on the first failing rule; the message
    /// is already in Hebrew and safe to surface to the caller.
    /// </summary>
    private static void Validate(
        string            name,
        string?           description,
        ControlDefinition definition)
    {
        // Req 1.7: Name is required (not empty / not whitespace-only)
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("שם הבקרה הוא שדה חובה.");

        // Req 1.1: Name length cap
        if (name.Length > MaxNameLength)
            throw new ArgumentException(
                $"שם הבקרה לא יכול לעלות על {MaxNameLength} תווים.");

        // Req 1.1: Description length cap
        if (description is not null && description.Length > MaxDescriptionLength)
            throw new ArgumentException(
                $"תיאור הבקרה לא יכול לעלות על {MaxDescriptionLength} תווים.");

        // Req 1.3, 1.9: At least one condition must be present
        if (definition.Conditions.Count == 0)
            throw new ArgumentException("נדרש לפחות תנאי אחד להגדרת הבקרה.");
    }

    /// <summary>
    /// Deserializes <paramref name="json"/> stored in <c>DefinitionJson</c> back to a
    /// <see cref="ControlDefinition"/>.
    /// <para>
    /// Throws <see cref="InvalidOperationException"/> (not <see cref="JsonException"/>)
    /// so the global middleware classifies it as a QueryBuild error rather than a user error.
    /// </para>
    /// </summary>
    private static ControlDefinition Deserialize(string json, int controlId)
    {
        try
        {
            return JsonSerializer.Deserialize<ControlDefinition>(json, _jsonOptions)
                ?? throw new InvalidOperationException(
                    $"DefinitionJson של בקרה {controlId} ריק לאחר deserialization.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"DefinitionJson של בקרה {controlId} פגום ואינו ניתן לפענוח.", ex);
        }
    }
}
