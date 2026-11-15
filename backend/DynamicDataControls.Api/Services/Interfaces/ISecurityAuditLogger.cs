namespace DynamicDataControls.Api.Services.Interfaces;

/// <summary>
/// Records security-relevant events at Warning level for audit trail.
/// Requirements: 8.4, 8.5, 8.7
/// </summary>
public interface ISecurityAuditLogger
{
    /// <summary>
    /// Logs a detected DML/DDL keyword in a generated SQL statement.
    /// </summary>
    /// <param name="userId">Identifier of the authenticated user (JWT NameIdentifier claim).</param>
    /// <param name="attemptedSql">The SQL string that triggered the detection (must not be executed).</param>
    void LogDmlAttempt(string userId, string attemptedSql);

    /// <summary>
    /// Logs a suspected SQL-injection value found in a user-supplied field.
    /// </summary>
    /// <param name="userId">Identifier of the authenticated user.</param>
    /// <param name="fieldName">Name of the input field that contained the suspicious value.</param>
    /// <param name="value">The raw value that raised the alert.</param>
    void LogSqlInjectionAttempt(string userId, string fieldName, string value);

    /// <summary>
    /// Logs an attempt to access a resource for which the user is not authorised.
    /// </summary>
    /// <param name="userId">Identifier of the authenticated user.</param>
    /// <param name="resource">Description of the resource the user tried to access.</param>
    void LogUnauthorizedAccess(string userId, string resource);
}
