using DynamicDataControls.Api.Models.DTOs;
using DynamicDataControls.Api.Models.Entities;

namespace DynamicDataControls.Api.Services.Interfaces;

/// <summary>
/// Orchestrates the full lifecycle of a control execution:
/// SQL generation → ADO.NET execution against the target DB → result storage → history.
/// Requirements: 4.1, 4.2, 4.3, 4.5, 4.7, 4.8, 4.9, 5.7
/// </summary>
public interface IExecutionService
{
    /// <summary>
    /// Runs the control identified by <paramref name="controlId"/> end-to-end:
    /// loads its definition, builds and executes SQL against the target DB,
    /// stores up to 10,000 results in memory, and persists a
    /// <see cref="ControlExecution"/> audit row.
    /// </summary>
    /// <param name="controlId">Surrogate key of the <see cref="ControlEntity"/> to run.</param>
    /// <param name="ct">Optional cancellation token; cancels the in-flight SQL query.</param>
    /// <returns>Summary <see cref="ExecutionResult"/> including the new executionId and anomaly count.</returns>
    /// <exception cref="Services.Exceptions.NotFoundException">When <paramref name="controlId"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">When SQL generation fails (IsSuccess=false).</exception>
    Task<ExecutionResult> ExecuteAsync(int controlId, CancellationToken ct = default);

    /// <summary>
    /// Marks the execution <paramref name="executionId"/> as Cancelled in the audit log.
    /// </summary>
    /// <exception cref="Services.Exceptions.NotFoundException">When <paramref name="executionId"/> does not exist.</exception>
    Task CancelAsync(int executionId);

    /// <summary>
    /// Returns one page of results previously stored by <see cref="ExecuteAsync"/>.
    /// Results live in process memory and are not persisted across server restarts.
    /// </summary>
    /// <param name="executionId">The execution whose results to page through.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Records per page (default 100).</param>
    /// <exception cref="Services.Exceptions.NotFoundException">When results for <paramref name="executionId"/> are not in memory.</exception>
    Task<ResultsPage> GetResultsAsync(int executionId, int page, int pageSize = 100);

    /// <summary>Returns all execution history rows for the given control, newest first.</summary>
    Task<IReadOnlyList<ControlExecution>> GetHistoryAsync(int controlId);
}
