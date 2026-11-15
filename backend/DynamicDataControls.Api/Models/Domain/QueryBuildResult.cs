namespace DynamicDataControls.Api.Models.Domain;

/// <summary>
/// The outcome of a <c>QueryBuilderService.Build()</c> call.
/// On success, <see cref="IsSuccess"/> is <c>true</c> and both <see cref="Sql"/>
/// and <see cref="Parameters"/> are populated.
/// On failure, <see cref="IsSuccess"/> is <c>false</c> and <see cref="ErrorMessage"/>
/// contains a user-readable description of what went wrong.
/// Requirements: 3.1, 3.6
/// </summary>
/// <param name="IsSuccess">Whether SQL generation succeeded.</param>
/// <param name="Sql">The generated SQL string; null when <see cref="IsSuccess"/> is false.</param>
/// <param name="Parameters">
/// Named query parameters keyed <c>@p0</c>, <c>@p1</c>, …; null when <see cref="IsSuccess"/> is false
/// or when all conditions use parameterless operators (IsNull / IsNotNull).
/// </param>
/// <param name="ErrorMessage">
/// Human-readable error description; null when <see cref="IsSuccess"/> is true.
/// </param>
public record QueryBuildResult(
    bool IsSuccess,
    string? Sql,
    IReadOnlyDictionary<string, object>? Parameters,
    string? ErrorMessage);
