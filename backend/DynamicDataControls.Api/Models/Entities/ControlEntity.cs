namespace DynamicDataControls.Api.Models.Entities;

/// <summary>
/// EF Core entity for the Controls table.
/// Stores every control definition together with its condition set (serialised as JSON).
/// Requirements: 1.2
/// </summary>
public class ControlEntity
{
    /// <summary>Identity surrogate key.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable name, max 100 chars.
    /// Stored with Hebrew_CI_AS collation — uniqueness is enforced case-insensitively.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional free-text description, max 500 chars.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Full condition definition stored as a JSON document.
    /// Format matches ControlDefinition in the design document.
    /// </summary>
    public string DefinitionJson { get; set; } = string.Empty;

    /// <summary>UTC timestamp set once at INSERT.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp refreshed by the application on every UPDATE.</summary>
    public DateTime UpdatedAt { get; set; }

    // ── Navigation ────────────────────────────────────────────────────
    public ICollection<ControlExecution> Executions { get; set; } = new List<ControlExecution>();
}
