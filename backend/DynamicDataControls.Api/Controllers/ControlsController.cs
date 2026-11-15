using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DynamicDataControls.Api.Models.DTOs;
using DynamicDataControls.Api.Services.Interfaces;

namespace DynamicDataControls.Api.Controllers;

/// <summary>
/// CRUD endpoints for control definitions.
/// All endpoints require authentication; write operations additionally
/// require the ControlsManagerPolicy role.
/// Requirements: 1.1ג€“1.10, 8.6, 8.9
/// </summary>
[ApiController]
[Route("api/controls")]
[Authorize]
public class ControlsController : ControllerBase
{
    private readonly IControlsService _service;
    private readonly ILogger<ControlsController> _logger;

    public ControlsController(IControlsService service, ILogger<ControlsController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ג”€ג”€ GET /api/controls?search=q ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Returns all controls, optionally filtered by a case-insensitive substring
    /// on Name and Description.
    /// Sets the response header <c>X-Unexecuted-Controls: ׳˜׳¨׳ ׳”׳•׳¨׳¦׳”</c> when
    /// the result contains at least one control that has never been run.
    /// Requirements: 1.5, 1.6
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ControlListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var items = await _service.GetAllAsync(search);

        // Advisory header ג€” lets the UI distinguish "no executions yet" in a single
        // response without inspecting every row.  Requirements: 1.6
        if (items.Any(item => item.LastExecutedAt is null))
            Response.Headers["X-Unexecuted-Controls"] = "׳˜׳¨׳ ׳”׳•׳¨׳¦׳”";

        _logger.LogInformation(
            "GetAll controls | Search={Search} | Count={Count}",
            search, items.Count);

        return Ok(items);
    }

    // ג”€ג”€ GET /api/controls/{id} ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Returns the full detail of a single control including its condition definition.
    /// Requirements: 1.4, 1.5
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ControlDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        // Service throws NotFoundException (ג†’ 404) when control does not exist;
        // the global ExceptionHandlingMiddleware converts it automatically.
        var (entity, definition) = await _service.GetByIdAsync(id);

        // Derive LastExecutedAt from the navigation collection when it is loaded;
        // the collection is empty when executions were not eagerly included,
        // which correctly produces null (never executed).
        var lastExecutedAt = entity.Executions
            .OrderByDescending(e => e.ExecutedAt)
            .FirstOrDefault()?.ExecutedAt;

        var response = new ControlDetailResponse(
            entity.Id,
            entity.Name,
            entity.Description,
            definition,
            entity.CreatedAt,
            entity.UpdatedAt,
            lastExecutedAt);

        return Ok(response);
    }

    // ג”€ג”€ POST /api/controls ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Creates a new control.
    /// Requires the ControlsManager role.
    /// Returns 201 Created with the full detail of the new control.
    /// Requirements: 1.1, 1.3, 8.6
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ControlsManagerPolicy")]
    [ProducesResponseType(typeof(ControlDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateControlRequest request)
    {
        // Service throws ArgumentException (ג†’ 400) or ConflictException (ג†’ 409) on failure;
        // the global middleware converts them without any try/catch here.
        var entity = await _service.CreateAsync(request);

        // Use the validated definition from the request ג€” the service has already
        // accepted it, so this avoids a second DB round-trip.
        var response = new ControlDetailResponse(
            entity.Id,
            entity.Name,
            entity.Description,
            request.Definition,
            entity.CreatedAt,
            entity.UpdatedAt,
            LastExecutedAt: null);

        _logger.LogInformation(
            "Control created | Id={Id} | Name={Name} | User={User}",
            entity.Id, entity.Name,
            User.Identity?.Name ?? "unknown");

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    // ג”€ג”€ PUT /api/controls/{id} ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Updates an existing control.
    /// Requires the ControlsManager role.
    /// Requirements: 1.4, 1.7, 1.8, 1.9, 8.9
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ControlsManagerPolicy")]
    [ProducesResponseType(typeof(ControlDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateControlRequest request)
    {
        var entity = await _service.UpdateAsync(id, request);

        var lastExecutedAt = entity.Executions
            .OrderByDescending(e => e.ExecutedAt)
            .FirstOrDefault()?.ExecutedAt;

        var response = new ControlDetailResponse(
            entity.Id,
            entity.Name,
            entity.Description,
            request.Definition,
            entity.CreatedAt,
            entity.UpdatedAt,
            lastExecutedAt);

        _logger.LogInformation(
            "Control updated | Id={Id} | Name={Name} | User={User}",
            entity.Id, entity.Name,
            User.Identity?.Name ?? "unknown");

        return Ok(response);
    }

    // ג”€ג”€ DELETE /api/controls/{id} ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Deletes a control.
    /// Requires the ControlsManager role.
    /// Requirements: 1.10, 8.9
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ControlsManagerPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);

        _logger.LogInformation(
            "Control deleted | Id={Id} | User={User}",
            id, User.Identity?.Name ?? "unknown");

        return NoContent();
    }
}


