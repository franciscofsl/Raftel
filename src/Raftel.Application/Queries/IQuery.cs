using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Queries;

public interface IQuery<TResult> : IRequest<Result<TResult>>;