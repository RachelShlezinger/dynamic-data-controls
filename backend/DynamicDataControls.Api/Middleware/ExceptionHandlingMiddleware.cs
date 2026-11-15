using System.Security.Claims;
using System.Text.Json;
using DynamicDataControls.Api.Models.Domain;
using DynamicDataControls.Api.Services.Exceptions;
using Microsoft.AspNetCore.Http.Features;

namespace DynamicDataControls.Api.Middleware;

/// <summary>
/// Global exception-handling middleware.
/// Catches every unhandled exception, logs it with structured context,
/// and returns a generic Hebrew error response with no technical details exposed.
/// Requirements: 4.6, 8.4, 8.5
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    // Reusable serialiser options — avoids allocating on every error response.
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next    = next;
        _logger  = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (NotFoundException ex)
        {
            // Service-layer 404 — message is already a safe Hebrew string.
            await HandleExceptionAsync(httpContext, ex, StatusCodes.Status404NotFound, ErrorType.General, ex.Message);
        }
        catch (ConflictException ex)
        {
            // Service-layer 409 — message is already a safe Hebrew string.
            await HandleExceptionAsync(httpContext, ex, StatusCodes.Status409Conflict, ErrorType.General, ex.Message);
        }
        catch (ArgumentException ex)
        {
            // Service-layer 400 — message is already a safe Hebrew string.
            await HandleExceptionAsync(httpContext, ex, StatusCodes.Status400BadRequest, ErrorType.General, ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            // Framework-level bad-request (e.g. malformed JSON body, oversized payload).
            await HandleExceptionAsync(httpContext, ex, ex.StatusCode, ErrorType.General);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex, StatusCodes.Status500InternalServerError, ClassifyError(ex));
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    private async Task HandleExceptionAsync(
        HttpContext httpContext,
        Exception   exception,
        int         statusCode,
        ErrorType   errorType,
        string?     userMessage = null)
    {
        // --- Extract context values ---
        var userId    = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var controlId = httpContext.Request.RouteValues.TryGetValue("id", out var idVal)
                            ? idVal?.ToString()
                            : null;

        // --- Structured log — full technical details go here, never to the caller ---
        _logger.LogError(
            exception,
            "Unhandled exception | Timestamp={Timestamp} | ControlId={ControlId} | UserId={UserId} | ErrorType={ErrorType} | TechnicalMessage={TechnicalMessage}",
            DateTimeOffset.UtcNow,
            controlId ?? "(none)",
            userId,
            errorType,
            exception.Message);

        // --- Generic Hebrew response — no stack trace, no SQL ---
        if (!httpContext.Response.HasStarted)
        {
            httpContext.Response.StatusCode  = statusCode;
            httpContext.Response.ContentType = "application/json; charset=utf-8";

            // userMessage: safe Hebrew string provided by the service layer for 4xx errors.
            // For 5xx (and framework-level 400 without an explicit message), fall back to
            // a generic Hebrew phrase that leaks no technical details.
            string message = userMessage
                ?? (statusCode < StatusCodes.Status500InternalServerError
                    ? "הבקשה אינה תקינה. בדוק את הנתונים שהוזנו."
                    : "אירעה שגיאה. אנא פנה למנהל המערכת");

            var body = new { error = message };

            await httpContext.Response.WriteAsync(
                JsonSerializer.Serialize(body, _jsonOptions));
        }
    }

    /// <summary>
    /// Provides a best-effort classification of an exception into an <see cref="ErrorType"/>.
    /// </summary>
    private static ErrorType ClassifyError(Exception ex) =>
        ex switch
        {
            Microsoft.EntityFrameworkCore.DbUpdateException      => ErrorType.Database,
            Microsoft.Data.SqlClient.SqlException                => ErrorType.Database,
            HttpRequestException                                 => ErrorType.ExternalService,
            TaskCanceledException                                => ErrorType.ExternalService,
            InvalidOperationException ioe
                when ioe.Message.Contains("Query", StringComparison.OrdinalIgnoreCase)
                                                                 => ErrorType.QueryBuild,
            _                                                    => ErrorType.General
        };
}
