namespace DynamicDataControls.Api.Models.Entities;

/// <summary>
/// EF Core entity for the ControlExecutions table.
/// Immutable audit log — one row per control run.
/// The application INSERTs with Status='Running', then UPDATEs Status,
/// AnomalyCount and RunTimeMs when the run ends.
/// Requirements: 4.3
/// </summary>
public class ControlExecution
{
    /// <summary>Identity surrogate key.</summary>
    public int Id { get; set; }

    /// <summary>FK to the <see cref="ControlEntity"/> that was executed.</summary>
    public int ControlId { get; set; }

    /// <summary>UTC timestamp of when the run started.</summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>Number of anomalous records found. NULL while running.</summary>
    public int? AnomalyCount { get; set; }

    /// <summary>Wall-clock execution time in milliseconds. NULL while running.</summary>
    public int? RunTimeMs { get; set; }

    /// <summary>
    /// Lifecycle state of the run.
    /// Allowed values: Running | Success | Error | Cancelled
    /// Enforced by a CHECK constraint in the database.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Full technical error details. Never exposed to the UI.
    /// Requirements: 4.6, 8.4 — log internally, return generic message to caller.
    /// </summary>
    public string? ErrorMessage { get; set; }

    // ── Navigation ────────────────────────────────────────────────────
    public ControlEntity Control { get; set; } = null!;
}
