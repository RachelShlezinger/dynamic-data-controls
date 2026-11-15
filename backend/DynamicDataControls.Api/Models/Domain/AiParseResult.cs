namespace DynamicDataControls.Api.Models.Domain;

/// <summary>
/// The outcome of an <c>AiProxyService.ParseDescriptionAsync()</c> call.
/// <list type="bullet">
///   <item>On success: <see cref="IsSuccess"/> is <c>true</c> and <see cref="Definition"/> is populated.</item>
///   <item>On schema validation failure: <see cref="IsSuccess"/> is <c>false</c> and
///   <see cref="MissingFields"/> lists every failing field path (e.g. "conditions[0].fieldName").</item>
///   <item>On network / availability error: <see cref="IsSuccess"/> is <c>false</c> and
///   <see cref="ErrorMessage"/> contains a Hebrew user-facing message.</item>
/// </list>
/// Requirements: 7.3, 7.8, 7.11
/// </summary>
/// <param name="IsSuccess">Whether the AI parse and schema validation succeeded.</param>
/// <param name="Definition">The mapped <see cref="ControlDefinition"/>; null when <see cref="IsSuccess"/> is false.</param>
/// <param name="ErrorMessage">
/// Hebrew user-facing error description; null when <see cref="IsSuccess"/> is true
/// or when failure was due to schema validation (see <see cref="MissingFields"/>).
/// </param>
/// <param name="MissingFields">
/// List of field paths that failed schema validation (e.g. "controlName", "conditions[0].fieldName");
/// null when the failure was not a schema validation error or when <see cref="IsSuccess"/> is true.
/// </param>
public record AiParseResult(
    bool IsSuccess,
    ControlDefinition? Definition,
    string? ErrorMessage,
    IReadOnlyList<string>? MissingFields);
