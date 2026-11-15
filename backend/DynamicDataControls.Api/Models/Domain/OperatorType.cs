namespace DynamicDataControls.Api.Models.Domain;

/// <summary>
/// Comparison operators available in a control condition.
/// Each value maps to a SQL expression in QueryBuilderService.
/// Requirements: 2.3, 3.1
/// </summary>
public enum OperatorType
{
    /// <summary>col = @pN</summary>
    Equal,

    /// <summary>col &lt;&gt; @pN</summary>
    NotEqual,

    /// <summary>col &gt; @pN  — Number / Date only</summary>
    GreaterThan,

    /// <summary>col &lt; @pN  — Number / Date only</summary>
    LessThan,

    /// <summary>col &gt;= @pN  — Number / Date only</summary>
    GreaterThanOrEqual,

    /// <summary>col &lt;= @pN  — Number / Date only</summary>
    LessThanOrEqual,

    /// <summary>col LIKE '%v%'  — String only</summary>
    Contains,

    /// <summary>col LIKE 'v%'  — String only</summary>
    StartsWith,

    /// <summary>col IS NULL  — no parameter emitted</summary>
    IsNull,

    /// <summary>col IS NOT NULL  — no parameter emitted</summary>
    IsNotNull
}
