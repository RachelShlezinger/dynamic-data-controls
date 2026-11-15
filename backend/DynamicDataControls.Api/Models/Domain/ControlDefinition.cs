namespace DynamicDataControls.Api.Models.Domain;

/// <summary>
/// The complete, deserialized definition of a control — stored as JSON in
/// <c>Controls.DefinitionJson</c> and exchanged with the Angular frontend.
/// <para>
/// Lists are deep-compared in <see cref="Equals"/> so that two independently
/// deserialized instances with identical content test as equal (required for
/// the JSON round-trip property test).
/// </para>
/// Requirements: 2.8, 3.7
/// </summary>
public record ControlDefinition : IEquatable<ControlDefinition>
{
    /// <summary>Friendly name of the main entity whose table drives the FROM clause.</summary>
    public string PrimaryEntity { get; init; } = string.Empty;

    /// <summary>
    /// Friendly names of additional entities joined to the primary entity.
    /// Empty list means single-entity query (no JOINs).
    /// </summary>
    public IReadOnlyList<string> AdditionalEntities { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Ordered list of conditions that build the WHERE clause.
    /// Must contain at least one item.
    /// </summary>
    public IReadOnlyList<ConditionDefinition> Conditions { get; init; } = Array.Empty<ConditionDefinition>();

    // ── Structural equality ───────────────────────────────────────────────────
    // Records auto-generate Equals using reference equality for IReadOnlyList<T>,
    // so we override to perform deep (element-by-element) list comparison.

    /// <inheritdoc />
    public virtual bool Equals(ControlDefinition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return PrimaryEntity == other.PrimaryEntity
            && AdditionalEntities.SequenceEqual(other.AdditionalEntities, StringComparer.Ordinal)
            && Conditions.SequenceEqual(other.Conditions);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(PrimaryEntity);
        foreach (var entity in AdditionalEntities)
            hash.Add(entity);
        foreach (var condition in Conditions)
            hash.Add(condition);
        return hash.ToHashCode();
    }
}
