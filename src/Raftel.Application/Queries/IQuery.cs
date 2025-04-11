using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Queries;

/// <summary>
/// Represents a query that returns a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result produced by the query.</typeparam>
public interface IQuery<TResult> : IRequest<Result<TResult>>;