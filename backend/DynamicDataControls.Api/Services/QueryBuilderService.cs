using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicDataControls.Api.Models.Domain;
using DynamicDataControls.Api.Models.Entities;
using DynamicDataControls.Api.Services.Interfaces;

namespace DynamicDataControls.Api.Services;

/// <summary>
/// Translates a <see cref="ControlDefinition"/> into a parameterised SQL SELECT
/// statement using the provided metadata whitelist for name resolution.
/// <para>
/// Single-entity queries use alias <c>e0</c>.
/// Multi-entity queries add aliases <c>e1</c>, <c>e2</c>, … up to <c>e9</c>
/// (maximum 10 entities total), generating an INNER JOIN per additional entity.
/// </para>
/// Requirements: 3.1, 3.2, 3.3, 3.4, 3.8, 4.1
/// </summary>
public sealed class QueryBuilderService : IQueryBuilderService
{
    // ── Constants ─────────────────────────────────────────────────────────────

    private const int MaxEntities = 10;

    /// <summary>Regex that detects DML/DDL keywords as whole words (case-insensitive).</summary>
    private static readonly Regex DmlPattern = new(
        @"\b(INSERT|UPDATE|DELETE|DROP|TRUNCATE|ALTER|EXEC)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromSeconds(2));

    // ── Dependencies ──────────────────────────────────────────────────────────

    private readonly ISecurityAuditLogger _auditLogger;

    public QueryBuilderService(ISecurityAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    // A Value of "{{fieldName}}" means "compare against that column"
    // rather than a literal — no parameter is emitted.
    private const string FieldRefOpen  = "{{";
    private const string FieldRefClose = "}}";

    /// <inheritdoc/>
    public QueryBuildResult Build(
        ControlDefinition definition,
        IReadOnlyList<BusinessEntityModel> metadata,
        IReadOnlyList<EntityRelationship>? relationships = null)
    {
        // ── 1. Resolve primary entity ─────────────────────────────────────────
        var primaryEntity = FindEntity(metadata, definition.PrimaryEntity);
        if (primaryEntity is null)
            return Failure($"ישות לא נמצאה במטאדטה: {definition.PrimaryEntity}");

        if (definition.AdditionalEntities.Count >= MaxEntities)
            return Failure($"מספר הישויות חורג מהמקסימום המותר ({MaxEntities})");

        // ── 2. Build alias map  { friendlyName → (alias, entity) } ───────────
        // Index 0 = primary, indices 1..N = additional entities in declaration order.
        var aliasMap = new Dictionary<string, (string Alias, BusinessEntityModel Entity)>(
            StringComparer.OrdinalIgnoreCase)
        {
            [primaryEntity.FriendlyName] = ("e0", primaryEntity)
        };

        var joinClauses = new List<string>(definition.AdditionalEntities.Count);

        for (int i = 0; i < definition.AdditionalEntities.Count; i++)
        {
            var addEntityName = definition.AdditionalEntities[i];
            var addEntity = FindEntity(metadata, addEntityName);
            if (addEntity is null)
                return Failure($"ישות לא נמצאה במטאדטה: {addEntityName}");

            string addAlias = $"e{i + 1}";

            // ── 3. Find relationship between primary and additional entity ─────
            var rel = FindRelationship(relationships, primaryEntity.Id, addEntity.Id);
            if (rel is null)
                return Failure(
                    $"לא נמצא קשר (JOIN) בין הישויות: {definition.PrimaryEntity} ו-{addEntityName}");

            // Determine which side of the relationship is source/target.
            // The relationship may be stored with the primary as source OR as target.
            string sourceCol;
            string targetCol;

            if (rel.SourceEntityId == primaryEntity.Id)
            {
                // primary → additional
                var srcField = FindFieldById(primaryEntity, rel.SourceFieldId);
                var tgtField = FindFieldById(addEntity, rel.TargetFieldId);

                if (srcField is null)
                    return Failure($"שדה מקור של הקשר לא נמצא (Id={rel.SourceFieldId})");
                if (tgtField is null)
                    return Failure($"שדה יעד של הקשר לא נמצא (Id={rel.TargetFieldId})");

                sourceCol = srcField.ColumnName;
                targetCol = tgtField.ColumnName;

                joinClauses.Add(
                    $"INNER JOIN [{addEntity.TableName}] AS {addAlias}" +
                    $" ON e0.[{sourceCol}] = {addAlias}.[{targetCol}]");
            }
            else
            {
                // additional → primary (relationship stored with primary as target)
                var srcField = FindFieldById(addEntity, rel.SourceFieldId);
                var tgtField = FindFieldById(primaryEntity, rel.TargetFieldId);

                if (srcField is null)
                    return Failure($"שדה מקור של הקשר לא נמצא (Id={rel.SourceFieldId})");
                if (tgtField is null)
                    return Failure($"שדה יעד של הקשר לא נמצא (Id={rel.TargetFieldId})");

                sourceCol = srcField.ColumnName;
                targetCol = tgtField.ColumnName;

                joinClauses.Add(
                    $"INNER JOIN [{addEntity.TableName}] AS {addAlias}" +
                    $" ON {addAlias}.[{sourceCol}] = e0.[{targetCol}]");
            }

            aliasMap[addEntity.FriendlyName] = (addAlias, addEntity);
        }

        // ── 4. Build SELECT column list (all entities) ────────────────────────
        var columnParts = new List<string>();
        foreach (var (alias, entity) in aliasMap.Values)
        {
            if (entity.Fields.Count > 0)
                columnParts.AddRange(
                    entity.Fields.Select(f => $"{alias}.[{f.ColumnName}]"));
            else
                columnParts.Add($"{alias}.*");
        }

        var columnList = string.Join(", ", columnParts);

        // ── 5. Build WHERE clause ─────────────────────────────────────────────
        var parameters = new Dictionary<string, object>();
        var whereParts  = new List<string>(definition.Conditions.Count * 2);
        int paramIndex  = 0;

        for (int i = 0; i < definition.Conditions.Count; i++)
        {
            var cond = definition.Conditions[i];

            // Resolve the entity alias for this condition
            if (!aliasMap.TryGetValue(cond.EntityName, out var condEntry))
                return Failure($"ישות לא נמצאה במטאדטה: {cond.EntityName}");

            var (condAlias, condEntity) = condEntry;

            var condField = FindField(condEntity, cond.FieldName);
            if (condField is null)
                return Failure($"שדה לא נמצא במטאדטה: {cond.FieldName}");

            string colRef = $"{condAlias}.[{condField.ColumnName}]";

            var fragment = BuildConditionFragment(
                cond, condEntity, colRef, aliasMap, metadata,
                parameters, ref paramIndex,
                out string? fragmentError);

            if (fragment is null)
                return Failure(fragmentError!);

            whereParts.Add(fragment);

            if (i < definition.Conditions.Count - 1 && cond.LogicalOperator.HasValue)
                whereParts.Add(cond.LogicalOperator.Value == LogicalOperator.And
                    ? "AND"
                    : "OR");
        }

        // ── 6. Assemble final SQL ─────────────────────────────────────────────
        var sb = new StringBuilder();
        sb.Append($"SELECT TOP 10001 {columnList}");
        sb.Append($" FROM [{primaryEntity.TableName}] AS e0");

        foreach (var join in joinClauses)
        {
            sb.Append(' ');
            sb.Append(join);
        }

        if (whereParts.Count > 0)
        {
            sb.Append(" WHERE ");
            sb.Append(string.Join(" ", whereParts));
        }

        // ── 7. Post-build security guard (Requirements: 3.3, 8.1, 8.7) ──────────
        var sql = sb.ToString();

        // Guard 1: generated SQL must start with SELECT
        if (!sql.TrimStart().StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
        {
            _auditLogger.LogDmlAttempt("system", sql);
            return Failure("שאילתה שנוצרה אינה מסוג SELECT — נדחתה מטעמי אבטחה");
        }

        // Guard 2: no DML/DDL keywords permitted anywhere in the statement
        if (DmlPattern.IsMatch(sql))
        {
            _auditLogger.LogDmlAttempt("system", sql);
            return Failure("שאילתה שנוצרה מכילה מילת מפתח אסורה — נדחתה מטעמי אבטחה");
        }

        return new QueryBuildResult(
            IsSuccess:    true,
            Sql:          sql,
            Parameters:   parameters.Count > 0 ? parameters : null,
            ErrorMessage: null);
    }

    // ── Condition fragment ────────────────────────────────────────────────────

    /// <summary>
    /// Builds the SQL fragment for a single condition, adding any required
    /// parameter entry to <paramref name="parameters"/>.
    /// Returns <c>null</c> (and sets <paramref name="error"/>) on failure.
    /// </summary>
    private static string? BuildConditionFragment(
        ConditionDefinition cond,
        BusinessEntityModel condEntity,
        string colRef,
        IReadOnlyDictionary<string, (string Alias, BusinessEntityModel Entity)> aliasMap,
        IReadOnlyList<BusinessEntityModel> metadata,
        Dictionary<string, object> parameters,
        ref int paramIndex,
        out string? error)
    {
        error = null;

        // ── Parameterless operators ───────────────────────────────────────────
        if (cond.Operator == OperatorType.IsNull)
            return $"{colRef} IS NULL";

        if (cond.Operator == OperatorType.IsNotNull)
            return $"{colRef} IS NOT NULL";

        // ── Cross-field reference  {{FriendlyFieldName}} ──────────────────────
        // If the value is a string wrapped in {{ }}, treat it as a column reference
        // rather than a literal — no parameter is emitted.
        if (cond.Value.HasValue &&
            cond.Value.Value.ValueKind == JsonValueKind.String)
        {
            var strVal = cond.Value.Value.GetString() ?? string.Empty;

            if (strVal.StartsWith(FieldRefOpen, StringComparison.Ordinal) &&
                strVal.EndsWith(FieldRefClose, StringComparison.Ordinal))
            {
                var refName = strVal[FieldRefOpen.Length..^FieldRefClose.Length];

                // Search the condition's own entity first, then all entities in
                // the alias map, then fall back to the full metadata list.
                BusinessField? refField = FindField(condEntity, refName);

                if (refField is null)
                {
                    foreach (var (alias, entity) in aliasMap.Values)
                    {
                        refField = FindField(entity, refName);
                        if (refField is not null)
                        {
                            // Found in a known aliased entity — emit the qualified ref
                            string refColRef = $"{alias}.[{refField.ColumnName}]";
                            return $"{colRef} {OperatorToSql(cond.Operator)} {refColRef}";
                        }
                    }

                    // Last resort: search all metadata (entity may not be in aliasMap)
                    refField = metadata.SelectMany(e => e.Fields)
                                       .FirstOrDefault(f =>
                                           string.Equals(f.FriendlyName, refName,
                                               StringComparison.OrdinalIgnoreCase));
                }

                if (refField is null)
                {
                    error = $"שדה ייחוס לא נמצא במטאדטה: {refName}";
                    return null;
                }

                // If we got here via the same-entity path, determine its alias
                var owningEntry = aliasMap.Values.FirstOrDefault(e =>
                    e.Entity.Fields.Any(f => f.Id == refField.Id));

                string resolvedAlias = owningEntry != default
                    ? owningEntry.Alias
                    : "e0"; // safe default for single-entity queries

                return $"{colRef} {OperatorToSql(cond.Operator)} {resolvedAlias}.[{refField.ColumnName}]";
            }
        }

        // ── Standard parameterised condition ──────────────────────────────────
        var paramName  = $"@p{paramIndex++}";
        var paramValue = ExtractValue(cond.Value);

        if (cond.Operator == OperatorType.Contains && paramValue is string cv)
            parameters[paramName] = $"%{cv}%";
        else if (cond.Operator == OperatorType.StartsWith && paramValue is string sv)
            parameters[paramName] = $"{sv}%";
        else
            parameters[paramName] = paramValue ?? DBNull.Value;

        return $"{colRef} {OperatorToSql(cond.Operator)} {paramName}";
    }

    // ── Operator mapping ──────────────────────────────────────────────────────

    private static string OperatorToSql(OperatorType op) => op switch
    {
        OperatorType.Equal              => "=",
        OperatorType.NotEqual           => "<>",
        OperatorType.GreaterThan        => ">",
        OperatorType.LessThan           => "<",
        OperatorType.GreaterThanOrEqual => ">=",
        OperatorType.LessThanOrEqual    => "<=",
        OperatorType.Contains           => "LIKE",
        OperatorType.StartsWith         => "LIKE",
        _ => throw new ArgumentOutOfRangeException(nameof(op),
                 $"Operator {op} has no scalar SQL equivalent.")
    };

    // ── JsonElement value extraction ──────────────────────────────────────────

    private static object? ExtractValue(JsonElement? element)
    {
        if (!element.HasValue || element.Value.ValueKind == JsonValueKind.Null)
            return null;

        return element.Value.ValueKind switch
        {
            JsonValueKind.String => element.Value.GetString(),
            JsonValueKind.Number =>
                element.Value.TryGetInt64(out var l)   ? (object)l  :
                element.Value.TryGetDecimal(out var d) ? d          :
                (object)element.Value.GetDouble(),
            JsonValueKind.True  => true,
            JsonValueKind.False => false,
            _                   => element.Value.GetRawText()
        };
    }

    // ── Metadata lookup helpers ───────────────────────────────────────────────

    private static BusinessEntityModel? FindEntity(
        IReadOnlyList<BusinessEntityModel> metadata,
        string friendlyName) =>
        metadata.FirstOrDefault(e =>
            string.Equals(e.FriendlyName, friendlyName,
                StringComparison.OrdinalIgnoreCase));

    private static BusinessField? FindField(
        BusinessEntityModel entity,
        string friendlyName) =>
        entity.Fields.FirstOrDefault(f =>
            string.Equals(f.FriendlyName, friendlyName,
                StringComparison.OrdinalIgnoreCase));

    private static BusinessField? FindFieldById(
        BusinessEntityModel entity,
        int fieldId) =>
        entity.Fields.FirstOrDefault(f => f.Id == fieldId);

    /// <summary>
    /// Finds a relationship between two entities (by their IDs), regardless of
    /// which entity is stored as source vs. target.
    /// </summary>
    private static EntityRelationship? FindRelationship(
        IReadOnlyList<EntityRelationship>? relationships,
        int entityAId,
        int entityBId)
    {
        if (relationships is null) return null;

        return relationships.FirstOrDefault(r =>
            (r.SourceEntityId == entityAId && r.TargetEntityId == entityBId) ||
            (r.SourceEntityId == entityBId && r.TargetEntityId == entityAId));
    }

    // ── Result helpers ────────────────────────────────────────────────────────

    private static QueryBuildResult Failure(string message) =>
        new(IsSuccess: false, Sql: null, Parameters: null, ErrorMessage: message);
}
