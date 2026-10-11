namespace Raftel.Application.Abstractions;

/// <summary>
/// Runs work under an explicit correlation identifier when there is no HTTP request to supply one,
/// such as the outbox processor delivering an event captured by an earlier request.
/// </summary>
public interface ICorrelationScope
{
    /// <summary>
    /// Makes <paramref name="correlationId"/> the value of <see cref="ICorrelationContext.CorrelationId"/>
    /// until the returned scope is disposed. A null or blank identifier changes nothing.
    /// </summary>
    IDisposable Begin(string correlationId);
}
