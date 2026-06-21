namespace Raftel.Application.Abstractions.Auditing;

/// <summary>
/// Provides ambient access to the Raftel command or query that is currently being
/// processed, so that infrastructure components (e.g. an EF Core interceptor) can
/// attribute entity changes to the request that caused them.
/// </summary>
public interface IAuditLogScope
{
    /// <summary>
    /// Gets the full name of the command or query currently being processed, if any.
    /// </summary>
    string? Command { get; }

    /// <summary>
    /// Begins a new audit scope for the given command or query.
    /// </summary>
    /// <param name="command">The full name of the command or query being processed.</param>
    /// <returns>An <see cref="IDisposable"/> that, when disposed, reverts the scope change.</returns>
    IDisposable Begin(string command);
}
