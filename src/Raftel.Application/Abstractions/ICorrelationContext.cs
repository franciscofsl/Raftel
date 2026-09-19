namespace Raftel.Application.Abstractions;

/// <summary>
/// Exposes the correlation identifier resolved for the current HTTP request, so that any
/// component in the pipeline can tag work (e.g. audit entries, domain events) with it.
/// </summary>
public interface ICorrelationContext
{
    /// <summary>
    /// Gets the correlation identifier resolved for the current request: the sanitized
    /// <c>X-Correlation-Id</c> request header when present and valid, or a generated identifier
    /// otherwise.
    /// </summary>
    string CorrelationId { get; }
}
