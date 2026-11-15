namespace DynamicDataControls.Api.Models.Domain;

/// <summary>
/// Logical connector placed between consecutive conditions in a control definition.
/// The last condition in a list has no connector (null in JSON).
/// Requirements: 2.4
/// </summary>
public enum LogicalOperator
{
    /// <summary>Both conditions must be true (SQL: AND).</summary>
    And,

    /// <summary>Either condition must be true (SQL: OR).</summary>
    Or
}
