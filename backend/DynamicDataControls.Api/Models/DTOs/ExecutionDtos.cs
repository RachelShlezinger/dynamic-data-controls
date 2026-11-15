namespace DynamicDataControls.Api.Models.DTOs;

/// <summary>
/// Lifecycle state of a control execution run.
/// Must stay in sync with the CHECK constraint on ControlExecutions.Status.
/// Requirements: 4.3
/// </summary>
public enum ExecutionStatus
{
    Running,
    Success,
    Error,
    Cancelled
}

/// <summary>
/// Summary returned immediately after <c>ExecuteAsync</c> completes successfully.
/// Requirements: 4.1, 4.2
/// </summary>
/// <param name="ExecutionId">Surrogate key of the saved <see cref="Entities.ControlExecution"/> row.</param>
/// <param name="ControlId">The control that was run.</param>
/// <param name="Status">Final execution status.</param>
/// <param name="AnomalyCount">Number of records returned (capped at 10,000).</param>
/// <param name="IsTruncated">True when the query matched more than 10,000 rows.</param>
public record ExecutionResult(
    int             ExecutionId,
    int             ControlId,
    ExecutionStatus Status,
    int             AnomalyCount,
    bool            IsTruncated);

/// <summary>
/// Metadata for one column in an execution result set.
/// Always uses the business-friendly name — never the raw SQL column name.
/// Requirements: 5.1
/// </summary>
/// <param name="FriendlyName">Business-facing column name shown in the results table.</param>
/// <param name="DataType">One of: Number, String, Date, Boolean.</param>
public record ColumnDefinition(
    string FriendlyName,
    string DataType);

/// <summary>
/// One page of execution results returned by <c>GetResultsAsync</c>.
/// Requirements: 5.1–5.9
/// </summary>
/// <param name="ExecutionId">Which execution these results belong to.</param>
/// <param name="ControlId">The control that was run.</param>
/// <param name="TotalCount">Total stored records (at most 10,000).</param>
/// <param name="Page">The 1-based page number returned.</param>
/// <param name="PageSize">Records per page as requested.</param>
/// <param name="IsTruncated">True when the original query matched more than 10,000 rows.</param>
/// <param name="Columns">Ordered column metadata matching the keys in <see cref="Records"/>.</param>
/// <param name="Records">Records for this page; each row is a friendly-name → value dictionary.</param>
public record ResultsPage(
    int                                         ExecutionId,
    int                                         ControlId,
    int                                         TotalCount,
    int                                         Page,
    int                                         PageSize,
    bool                                        IsTruncated,
    IReadOnlyList<ColumnDefinition>             Columns,
    IReadOnlyList<IDictionary<string, object?>> Records);

// ─────────────────────────────────────────────────────────────────────────────
// ExecutionController DTOs
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Request body for <c>POST /api/executions</c>.
/// Requirements: 4.1
/// </summary>
/// <param name="ControlId">Surrogate key of the control to run.</param>
public record ExecuteRequest(int ControlId);

/// <summary>
/// Response body for <c>POST /api/executions</c>.
/// Extends <see cref="ExecutionResult"/> with an optional Hebrew truncation notice.
/// Requirements: 4.1, 4.2, 5.7
/// </summary>
/// <param name="ExecutionId">Surrogate key of the saved <see cref="Entities.ControlExecution"/> row.</param>
/// <param name="ControlId">The control that was run.</param>
/// <param name="Status">Final execution status.</param>
/// <param name="AnomalyCount">Number of anomalous records found (capped at 10,000).</param>
/// <param name="IsTruncated">True when the original query matched more than 10,000 rows.</param>
/// <param name="TruncationMessage">
/// Non-null Hebrew notice when <paramref name="IsTruncated"/> is <c>true</c>:
/// "התוצאות קוצצו ל-10,000 הרשומות הראשונות".
/// </param>
public record ExecutionResponse(
    int             ExecutionId,
    int             ControlId,
    string          Status,
    int             AnomalyCount,
    bool            IsTruncated,
    string?         TruncationMessage);

/// <summary>
/// One row of execution history returned by <c>GET /api/controls/{controlId}/executions</c>.
/// Requirements: 4.3
/// </summary>
/// <param name="ExecutionId">Surrogate key of the execution row.</param>
/// <param name="ControlId">The control that was run.</param>
/// <param name="ExecutedAt">UTC timestamp when the run started.</param>
/// <param name="Status">Terminal status: Running | Success | Error | Cancelled.</param>
/// <param name="AnomalyCount">Number of anomalous records found; null while still running.</param>
/// <param name="RunTimeMs">Wall-clock duration in milliseconds; null while still running.</param>
public record ExecutionHistoryItem(
    int       ExecutionId,
    int       ControlId,
    DateTime  ExecutedAt,
    string    Status,
    int?      AnomalyCount,
    int?      RunTimeMs);
