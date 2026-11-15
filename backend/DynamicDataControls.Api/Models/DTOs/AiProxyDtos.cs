using DynamicDataControls.Api.Models.Domain;

namespace DynamicDataControls.Api.Models.DTOs;

/// <summary>
/// DTOs used by <c>AiProxyController</c> for the /api/ai/* endpoints.
/// These are the Angular ↔ .NET contract; the internal .NET ↔ Python
/// contract lives in <c>AiDtos.cs</c>.
/// Requirements: 7.1, 7.5, 7.10
/// </summary>

// ── Incoming request ──────────────────────────────────────────────────────────

/// <summary>
/// Request body for <c>POST /api/ai/parse</c>.
/// </summary>
/// <param name="Description">Free-text description of the desired control (10–1,000 chars).</param>
public record ParseDescriptionRequest(string Description);

// ── Success response ──────────────────────────────────────────────────────────

/// <summary>
/// 200 OK body returned when the AI successfully parsed the description.
/// The frontend displays this for user review and confirmation before saving.
/// </summary>
/// <param name="ControlName">
/// Suggested name hint for the control, derived from the AI's
/// <c>controlName</c> value surfaced via <see cref="ControlDefinition.PrimaryEntity"/>.
/// </param>
/// <param name="Definition">
/// The structured <see cref="ControlDefinition"/> ready for user review.
/// </param>
public record AiParseSuccessResponse(string ControlName, ControlDefinition Definition);

// ── Error response ────────────────────────────────────────────────────────────

/// <summary>
/// Error body used for both 422 (schema validation) and 503 (service unavailable) responses.
/// </summary>
/// <param name="Error">Hebrew user-facing error message.</param>
/// <param name="MissingFields">
/// List of failing field paths (e.g. <c>"controlName"</c>, <c>"conditions[0].fieldName"</c>);
/// populated only for 422 schema-validation errors, <c>null</c> for 503 responses.
/// </param>
public record AiParseErrorResponse(string Error, IReadOnlyList<string>? MissingFields = null);
