namespace Raftel.Application.Abstractions;

/// <summary>
/// Represents a request that produces a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of response expected from the request.</typeparam>
public interface IRequest<TResponse> { }