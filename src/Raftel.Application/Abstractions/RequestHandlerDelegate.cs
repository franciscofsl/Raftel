namespace Raftel.Application.Abstractions;

/// <summary>
/// Delegate that represents the next handler or middleware in the request processing pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
/// <returns>The asynchronous result of the request pipeline execution.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
