using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions;

/// <summary>
/// Represents a middleware specific to query handling in the request pipeline.
/// Inherits from <see cref="IGlobalMiddleware{TRequest, TResponse}"/> with a response type of <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the query request.</typeparam>
/// <typeparam name="TResponse">The type of the query response.</typeparam>
public interface IQueryMiddleware<TRequest, TResponse> : IGlobalMiddleware<TRequest, Result<TResponse>>
    where TRequest : IQuery<TResponse>
{
}