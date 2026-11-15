using System.Text.Json;

namespace DynamicDataControls.Api.Models.Domain;

/// <summary>
/// Represents a single condition in a control's definition.
/// Serialized as one element in the "conditions" JSON array.
/// <para>
/// <c>Value</c> is kept as a raw <see cref="JsonElement"/> so it can hold any JSON
/// literal (number, string, boolean, null) without losing fidelity during round-trips.
/// </para>
/// Requirements: 2.8, 3.7
/// </summary>
public record ConditionDefinition : IEquatable<ConditionDefinition>
{
    /// <summary>Client-generated unique identifier for this condition within the definition.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Friendly entity name (e.g. "עובד") — resolved to a table alias by QueryBuilderService.</summary>
    public string EntityName { get; init; } = string.Empty;

    /// <summary>Friendly field name (e.g. "גיל") — resolved to a column name by QueryBuilderService.</summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>Comparison operator applied to <see cref="FieldName"/>.</summary>
    public OperatorType Operator { get; init; }

    /// <summary>
    /// Comparison value. Null for <see cref="OperatorType.IsNull"/> and
    /// <see cref="OperatorType.IsNotNull"/> operators.
    /// Stored as a raw <see cref="JsonElement"/> to preserve the exact JSON type
    /// (number vs. string vs. boolean) across serialization round-trips.
    /// </summary>
    public JsonElement? Value { get; init; }

    /// <summary>
    /// Connector to the NEXT condition. Null on the last condition in the list.
    /// </summary>
    public LogicalOperator? LogicalOperator { get; init; }

    // ── Structural equality ───────────────────────────────────────────────────
    // Records auto-generate Equals based on all properties, but JsonElement is a
    // struct whose equality is not deep (it compares internal document pointers).
    // We override Equals and GetHashCode to compare by raw JSON text instead.

    /// <inheritdoc />
    public virtual bool Equals(ConditionDefinition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Id            == other.Id
            && EntityName    == other.EntityName
            && FieldName     == other.FieldName
            && Operator      == other.Operator
            && LogicalOperator == other.LogicalOperator
            && JsonElementsEqual(Value, other.Value);
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(
        Id,
        EntityName,
        FieldName,
        Operator,
        LogicalOperator,
        Value.HasValue ? Value.Value.GetRawText() : null);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool JsonElementsEqual(JsonElement? a, JsonElement? b)
    {
        if (!a.HasValue && !b.HasValue) return true;
        if (!a.HasValue || !b.HasValue) return false;
        return a.Value.GetRawText() == b.Value.GetRawText();
    }
}
