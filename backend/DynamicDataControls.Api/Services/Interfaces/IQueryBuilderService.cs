using DynamicDataControls.Api.Models.Domain;
using DynamicDataControls.Api.Models.Entities;

namespace DynamicDataControls.Api.Services.Interfaces;

/// <summary>
/// Builds a parameterised SELECT SQL statement from a <see cref="ControlDefinition"/>
/// and a resolved metadata whitelist, without touching the database.
/// Requirements: 3.1, 3.2, 3.3, 4.1
/// </summary>
public interface IQueryBuilderService
{
    /// <summary>
    /// Translates the given <paramref name="definition"/> into a SQL SELECT statement.
    /// </summary>
    /// <param name="definition">
    /// The control definition to translate. At least one condition is required.
    /// </param>
    /// <param name="metadata">
    /// The full metadata whitelist — entities with their fields already loaded.
    /// Used to resolve friendly names to table/column names and to validate that
    /// every referenced entity and field actually exists.
    /// </param>
    /// <param name="relationships">
    /// Optional collection of <see cref="EntityRelationship"/> records used to build
    /// JOIN clauses when <see cref="ControlDefinition.AdditionalEntities"/> is non-empty.
    /// When <c>null</c>, single-entity queries still succeed; multi-entity queries will
    /// fail with <c>IsSuccess=false</c> if no matching relationship is found.
    /// </param>
    /// <returns>
    /// A <see cref="QueryBuildResult"/> with <c>IsSuccess=true</c> and a populated
    /// <c>Sql</c> + <c>Parameters</c> on success, or <c>IsSuccess=false</c> with an
    /// <c>ErrorMessage</c> containing the first unresolvable name on failure.
    /// </returns>
    QueryBuildResult Build(
        ControlDefinition definition,
        IReadOnlyList<BusinessEntityModel> metadata,
        IReadOnlyList<EntityRelationship>? relationships = null);
}
