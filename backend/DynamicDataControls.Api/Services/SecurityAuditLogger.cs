using DynamicDataControls.Api.Services.Interfaces;

namespace DynamicDataControls.Api.Services;

/// <summary>
/// Concrete implementation of <see cref="ISecurityAuditLogger"/>.
/// Writes structured Warning-level log entries via Serilog / <see cref="ILogger{T}"/>.
/// Each entry includes a UTC timestamp and event type so they can be filtered and
/// forwarded to a dedicated sink (e.g. a security SIEM).
/// Requirements: 8.4, 8.5, 8.7
/// </summary>
public sealed class SecurityAuditLogger : ISecurityAuditLogger
{
    private readonly ILogger<SecurityAuditLogger> _logger;

    public SecurityAuditLogger(ILogger<SecurityAuditLogger> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void LogDmlAttempt(string userId, string attemptedSql)
    {
        _logger.LogWarning(
            "SECURITY_AUDIT | Timestamp={Timestamp} | EventType={EventType} | UserId={UserId} | AttemptedSql={AttemptedSql}",
            DateTimeOffset.UtcNow,
            "DmlAttempt",
            userId,
            attemptedSql);
    }

    /// <inheritdoc/>
    public void LogSqlInjectionAttempt(string userId, string fieldName, string value)
    {
        _logger.LogWarning(
            "SECURITY_AUDIT | Timestamp={Timestamp} | EventType={EventType} | UserId={UserId} | FieldName={FieldName} | SuspiciousValue={SuspiciousValue}",
            DateTimeOffset.UtcNow,
            "SqlInjectionAttempt",
            userId,
            fieldName,
            value);
    }

    /// <inheritdoc/>
    public void LogUnauthorizedAccess(string userId, string resource)
    {
        _logger.LogWarning(
            "SECURITY_AUDIT | Timestamp={Timestamp} | EventType={EventType} | UserId={UserId} | Resource={Resource}",
            DateTimeOffset.UtcNow,
            "UnauthorizedAccess",
            userId,
            resource);
    }
}
