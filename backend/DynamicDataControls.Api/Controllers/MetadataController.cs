using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DynamicDataControls.Api.Models.DTOs;
using DynamicDataControls.Api.Services.Exceptions;
using DynamicDataControls.Api.Services.Interfaces;

namespace DynamicDataControls.Api.Controllers;

/// <summary>
/// REST API for managing metadata: business entities, fields, and relationships.
/// GET endpoints are open to all authenticated users.
/// POST endpoints are restricted to the SystemAdmin role.
/// Requirements: 2.1, 2.2, 6.1ג€“6.8
/// </summary>
[ApiController]
[Route("api/metadata")]
[Authorize]
public sealed class MetadataController : ControllerBase
{
    private readonly IMetadataService _metadata;
    private readonly ILogger<MetadataController> _logger;

    public MetadataController(
        IMetadataService metadata,
        ILogger<MetadataController> logger)
    {
        _metadata = metadata;
        _logger   = logger;
    }

    // ג”€ג”€ Entities ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Returns all business entities (without their fields ג€” use the /fields sub-route for those).
    /// Open to all authenticated users.
    /// </summary>
    /// <response code="200">List of entity summaries.</response>
    [HttpGet("entities")]
    [ProducesResponseType(typeof(IEnumerable<EntityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntities()
    {
        var entities = await _metadata.GetAllEntitiesAsync();

        var dtos = entities.Select(e => new EntityDto(
            e.Id,
            e.FriendlyName,
            e.TableName,
            e.Description));

        return Ok(dtos);
    }

    /// <summary>
    /// Creates a new business entity.
    /// Restricted to SystemAdmin role.
    /// </summary>
    /// <response code="201">Entity created successfully.</response>
    /// <response code="400">Validation failure (empty name, name too long, table not found).</response>
    /// <response code="409">An entity with the same FriendlyName already exists.</response>
    [HttpPost("entities")]
    [Authorize(Policy = "SystemAdminPolicy")]
    [ProducesResponseType(typeof(EntityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEntity([FromBody] CreateEntityRequest request)
    {
        try
        {
            var entity = await _metadata.CreateEntityAsync(
                request.FriendlyName,
                request.TableName,
                request.Description);

            var dto = new EntityDto(entity.Id, entity.FriendlyName, entity.TableName, entity.Description);

            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning("Conflict creating entity: {Message}", ex.Message);
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ג”€ג”€ Fields ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Returns all fields for the specified business entity.
    /// Open to all authenticated users.
    /// </summary>
    /// <response code="200">List of fields.</response>
    /// <response code="404">Entity not found.</response>
    [HttpGet("entities/{id:int}/fields")]
    [ProducesResponseType(typeof(IEnumerable<FieldDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntityFields(int id)
    {
        try
        {
            var fields = await _metadata.GetFieldsByEntityIdAsync(id);

            var dtos = fields.Select(f => new FieldDto(
                f.Id,
                f.EntityId,
                f.FriendlyName,
                f.ColumnName,
                f.DataType,
                f.Description));

            return Ok(dtos);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new field on the specified business entity.
    /// Restricted to SystemAdmin role.
    /// </summary>
    /// <response code="201">Field created successfully.</response>
    /// <response code="400">Validation failure (invalid DataType, empty name, etc.).</response>
    /// <response code="404">Parent entity not found.</response>
    /// <response code="409">A field with the same FriendlyName already exists in this entity.</response>
    [HttpPost("entities/{id:int}/fields")]
    [Authorize(Policy = "SystemAdminPolicy")]
    [ProducesResponseType(typeof(FieldDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateField(int id, [FromBody] CreateFieldRequest request)
    {
        try
        {
            var field = await _metadata.CreateFieldAsync(
                id,
                request.FriendlyName,
                request.ColumnName,
                request.DataType,
                request.Description);

            var dto = new FieldDto(
                field.Id,
                field.EntityId,
                field.FriendlyName,
                field.ColumnName,
                field.DataType,
                field.Description);

            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning("Conflict creating field on entity {EntityId}: {Message}", id, ex.Message);
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ג”€ג”€ Relationships ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€ג”€

    /// <summary>
    /// Returns all entity JOIN relationships.
    /// Open to all authenticated users.
    /// </summary>
    /// <response code="200">List of relationships.</response>
    [HttpGet("relationships")]
    [ProducesResponseType(typeof(IEnumerable<RelationshipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRelationships()
    {
        var relationships = await _metadata.GetAllRelationshipsAsync();

        var dtos = relationships.Select(r => new RelationshipDto(
            r.Id,
            r.SourceEntityId,
            r.TargetEntityId,
            r.SourceFieldId,
            r.TargetFieldId));

        return Ok(dtos);
    }

    /// <summary>
    /// Creates a new JOIN relationship between two business entities.
    /// Restricted to SystemAdmin role.
    /// </summary>
    /// <response code="201">Relationship created successfully.</response>
    /// <response code="400">A field does not belong to its declared entity.</response>
    /// <response code="404">Source or target entity not found.</response>
    [HttpPost("relationships")]
    [Authorize(Policy = "SystemAdminPolicy")]
    [ProducesResponseType(typeof(RelationshipDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRelationship([FromBody] CreateRelationshipRequest request)
    {
        try
        {
            var rel = await _metadata.CreateRelationshipAsync(
                request.SourceEntityId,
                request.TargetEntityId,
                request.SourceFieldId,
                request.TargetFieldId);

            var dto = new RelationshipDto(
                rel.Id,
                rel.SourceEntityId,
                rel.TargetEntityId,
                rel.SourceFieldId,
                rel.TargetFieldId);

            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}


