using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DynamicDataControls.Api.Models.DTOs;
using DynamicDataControls.Api.Services.Interfaces;

namespace DynamicDataControls.Api.Controllers;

/// <summary>
/// REST endpoints for running controls, cancelling in-flight executions,
/// paging through results, and querying execution history.
///
/// All endpoints require a valid JWT ג€” no additional role restriction beyond
/// basic authentication, so any authenticated user can run controls and view results.
///
/// Exception routing (no try/catch needed here):
///   NotFoundException  ג†’ 404  handled by ExceptionHandlingMiddleware
///   ConflictException  ג†’ 409  handled by ExceptionHandlingMiddleware
///
/// Requirements: 4.1ג€“4.9, 5.1ג€“5.9
/// </summary>
[ApiController]
[Route("api/executions")]
[Authorize]
public sealed class ExecutionController : ControllerBase
{
    private readonly IExecutionService          _executionService;
    private readonly ILogger<ExecutionController> _logger;

    public ExecutionController(
        IExecutionService             executionService,
        ILogger<ExecutionController>  logger)
    {
        _executionService = executionService;
        _logger           = logger;
    }

    // ג”€ג”€ POST /api/executions ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Triggers a new execution for the specified control.
    /// <list type="bullet">
    ///   <item>201 ג€“ execution completed; body contains anomaly count and optional truncation notice.</item>
    ///   <item>404 ג€“ control not found (NotFoundException from service).</item>
    ///   <item>409 ג€“ control already running (ConflictException from concurrent guard, Requirement 4.8).</item>
    /// </list>
    /// Requirements: 4.1, 4.2, 4.3, 4.8
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ExecutionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Execute(
        [FromBody]       ExecuteRequest request,
        CancellationToken              ct)
    {
        // NotFoundException  ג†’ middleware ג†’ 404
        // ConflictException  ג†’ middleware ג†’ 409  ("׳”׳¨׳¦׳” ׳›׳‘׳¨ ׳₪׳¢׳™׳׳” ׳¢׳‘׳•׳¨ ׳‘׳§׳¨׳” ׳–׳•")
        var result = await _executionService.ExecuteAsync(request.ControlId, ct);

        // Requirement 5.7: include the Hebrew truncation notice when results were capped.
        string? truncationMessage = result.IsTruncated
            ? "׳”׳×׳•׳¦׳׳•׳× ׳§׳•׳¦׳¦׳• ׳-10,000 ׳”׳¨׳©׳•׳׳•׳× ׳”׳¨׳׳©׳•׳ ׳•׳×"
            : null;

        var response = new ExecutionResponse(
            result.ExecutionId,
            result.ControlId,
            result.Status.ToString(),
            result.AnomalyCount,
            result.IsTruncated,
            truncationMessage);

        _logger.LogInformation(
            "Execution completed | ExecutionId={ExecutionId} | ControlId={ControlId} | " +
            "AnomalyCount={AnomalyCount} | IsTruncated={IsTruncated} | User={User}",
            result.ExecutionId, result.ControlId,
            result.AnomalyCount, result.IsTruncated,
            User.Identity?.Name ?? "unknown");

        // Location header points to the results endpoint for this execution.
        return CreatedAtAction(
            nameof(GetResults),
            new { id = result.ExecutionId, page = 1, pageSize = 100 },
            response);
    }

    // ג”€ג”€ DELETE /api/executions/{id} ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Cancels an in-flight execution.
    /// <list type="bullet">
    ///   <item>204 ג€“ cancellation signal sent and audit row updated.</item>
    ///   <item>404 ג€“ execution not found (NotFoundException from service).</item>
    /// </list>
    /// Requirements: 4.5, 4.9
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        // NotFoundException ג†’ middleware ג†’ 404
        await _executionService.CancelAsync(id);

        _logger.LogInformation(
            "Execution cancel requested | ExecutionId={ExecutionId} | User={User}",
            id, User.Identity?.Name ?? "unknown");

        return NoContent();
    }

    // ג”€ג”€ GET /api/executions/{id}/results ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Returns one page of results for a completed execution.
    /// Results live in process memory and are lost on server restart.
    /// <list type="bullet">
    ///   <item>200 ג€“ <see cref="ResultsPage"/> with friendly-name column headers.</item>
    ///   <item>404 ג€“ results not in memory (NotFoundException from service).</item>
    /// </list>
    /// Requirements: 5.1ג€“5.9
    /// </summary>
    [HttpGet("{id:int}/results")]
    [ProducesResponseType(typeof(ResultsPage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResults(
        int               id,
        [FromQuery] int   page     = 1,
        [FromQuery] int   pageSize = 100)
    {
        // NotFoundException ג†’ middleware ג†’ 404
        var results = await _executionService.GetResultsAsync(id, page, pageSize);
        return Ok(results);
    }

    // ג”€ג”€ GET /api/controls/{controlId}/executions ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Returns execution history for the specified control, newest first.
    /// Uses an absolute route to co-exist with the ControlsController's namespace.
    /// Requirements: 4.3
    /// </summary>
    [HttpGet("/api/controls/{controlId:int}/executions")]
    [ProducesResponseType(typeof(IReadOnlyList<ExecutionHistoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(int controlId)
    {
        var history = await _executionService.GetHistoryAsync(controlId);

        var items = history.Select(e => new ExecutionHistoryItem(
            e.Id,
            e.ControlId,
            e.ExecutedAt,
            e.Status,
            e.AnomalyCount,
            e.RunTimeMs));

        return Ok(items);
    }
}


