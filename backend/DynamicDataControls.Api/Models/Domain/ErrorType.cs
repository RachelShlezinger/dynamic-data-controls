namespace DynamicDataControls.Api.Models.Domain;

/// <summary>
/// Categorises the type of unhandled error captured by ExceptionHandlingMiddleware.
/// </summary>
public enum ErrorType
{
    /// <summary>Unexpected / uncategorised server-side exception.</summary>
    General,

    /// <summary>Database-level error (EF Core / ADO.NET).</summary>
    Database,

    /// <summary>Query-builder or SQL-generation failure.</summary>
    QueryBuild,

    /// <summary>Control execution runtime failure.</summary>
    Execution,

    /// <summary>External service (AI proxy) failure.</summary>
    ExternalService,

    /// <summary>Security violation (DML attempt, injection attempt, etc.).</summary>
    Security
}
