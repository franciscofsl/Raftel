namespace Raftel.Application.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to perform an operation without proper authorization.
/// </summary>
[Obsolete("The framework's own pipeline no longer throws this; PermissionAuthorizationMiddleware returns a " +
          "failed Result. Inspect Result instead of catching this. Will be removed in a future version.")]
public class UnauthorizedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UnauthorizedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a specified error message 
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}