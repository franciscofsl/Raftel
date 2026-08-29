namespace Raftel.Application.Abstractions;

/// <summary>
/// Delegate that represents the next handler or middleware in the request processing pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
/// <returns>The asynchronous result of the request pipeline execution.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);
