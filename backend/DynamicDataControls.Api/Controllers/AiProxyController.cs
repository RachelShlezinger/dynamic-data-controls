using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DynamicDataControls.Api.Models.DTOs;
using DynamicDataControls.Api.Services.Interfaces;

namespace DynamicDataControls.Api.Controllers;

/// <summary>
/// REST API for the AI-assisted control creation flow.
/// All endpoints require authentication; no additional role restriction is applied
/// because any authenticated user may request an AI parse.
/// Requirements: 7.1, 7.5, 7.10
/// </summary>
[ApiController]
[Route("api/ai")]
[Authorize]
public sealed class AiProxyController : ControllerBase
{
    private readonly IAiProxyService _aiProxy;
    private readonly ILogger<AiProxyController> _logger;

    public AiProxyController(
        IAiProxyService aiProxy,
        ILogger<AiProxyController> logger)
    {
        _aiProxy = aiProxy;
        _logger  = logger;
    }

    /// <summary>
    /// Sends a free-text description to the AI service and returns a structured
    /// <c>ControlDefinition</c> for user review and confirmation.
    /// The definition is NOT saved at this stage — the user must confirm and POST
    /// to <c>/api/controls</c> afterwards.
    /// </summary>
    /// <param name="request">Request body containing the description string.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">AI parsed successfully — <see cref="AiParseSuccessResponse"/> with the definition.</response>
    /// <response code="400">Description is missing or outside the 10–1,000 character range.</response>
    /// <response code="422">AI returned a response that failed schema validation; <c>missingFields</c> lists the offending paths.</response>
    /// <response code="503">Python AI service is unavailable or timed out.</response>
    [HttpPost("parse")]
    [ProducesResponseType(typeof(AiParseSuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AiParseErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AiParseErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(AiParseErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Parse(
        [FromBody] ParseDescriptionRequest request,
        CancellationToken ct)
    {
        // ── Server-side length validation ─────────────────────────────────────
        var description = request.Description?.Trim() ?? string.Empty;

        if (description.Length < 10)
        {
            return BadRequest(new AiParseErrorResponse(
                "נדרש תיאור מפורט יותר (לפחות 10 תווים)"));
        }

        if (description.Length > 1000)
        {
            return BadRequest(new AiParseErrorResponse(
                "התיאור ארוך מדי (מקסימום 1,000 תווים)"));
        }

        // ── Delegate to AiProxyService ────────────────────────────────────────
        var result = await _aiProxy.ParseDescriptionAsync(description, ct);

        if (result.IsSuccess)
        {
            // Use the AI's controlName hint surfaced through PrimaryEntity;
            // the full ControlDefinition is returned for the user to review.
            var response = new AiParseSuccessResponse(
                ControlName: result.Definition!.PrimaryEntity,
                Definition:  result.Definition);

            return Ok(response);
        }

        // Schema validation failure — 422 with the list of missing field paths
        if (result.MissingFields is { Count: > 0 })
        {
            _logger.LogWarning(
                "AI schema validation failed. Missing fields: {Fields}",
                string.Join(", ", result.MissingFields));

            return UnprocessableEntity(new AiParseErrorResponse(
                Error:         "AI schema validation failed",
                MissingFields: result.MissingFields));
        }

        // Service unavailable / network error — 503 with the Hebrew message
        _logger.LogWarning("AI service unavailable: {ErrorMessage}", result.ErrorMessage);

        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new AiParseErrorResponse(result.ErrorMessage ?? "שירות ה-AI אינו זמין"));
    }
}
