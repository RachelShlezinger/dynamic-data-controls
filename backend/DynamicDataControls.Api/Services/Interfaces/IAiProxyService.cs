using DynamicDataControls.Api.Models.Domain;

namespace DynamicDataControls.Api.Services.Interfaces;

/// <summary>
/// Communicates with the Python FastAPI AI service to parse a free-text description
/// into a structured <see cref="ControlDefinition"/>.
/// <para>
/// The service builds the entity/field context from the metadata layer, forwards
/// the request to the Python service, validates the response schema, and maps the
/// result to a <see cref="ControlDefinition"/> ready for user confirmation.
/// </para>
/// Requirements: 7.2, 7.3, 7.7, 7.8, 7.11
/// </summary>
public interface IAiProxyService
{
    /// <summary>
    /// Sends <paramref name="description"/> to the AI service, validates the response,
    /// and returns a <see cref="AiParseResult"/>.
    /// <list type="bullet">
    ///   <item>On success — <see cref="AiParseResult.IsSuccess"/> is <c>true</c> and
    ///   <see cref="AiParseResult.Definition"/> is populated.</item>
    ///   <item>On schema validation failure — <see cref="AiParseResult.IsSuccess"/> is <c>false</c>
    ///   and <see cref="AiParseResult.MissingFields"/> lists every failing field path.</item>
    ///   <item>On network / timeout error — <see cref="AiParseResult.IsSuccess"/> is <c>false</c>
    ///   and <see cref="AiParseResult.ErrorMessage"/> contains a Hebrew user-facing message.</item>
    /// </list>
    /// </summary>
    /// <param name="description">Free-text description of the desired control (10–1,000 chars).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AiParseResult> ParseDescriptionAsync(string description, CancellationToken ct = default);
}
