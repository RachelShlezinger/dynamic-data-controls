namespace DynamicDataControls.Api.Services.Exceptions;

/// <summary>
/// Thrown when an operation would violate a uniqueness constraint.
/// Maps to HTTP 409 in the exception-handling middleware.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException) { }
}
