namespace Raftel.Application.Abstractions;

public interface IRequestDispatcher
{
    Task<TResponse> DispatchAsync<TRequest, TResponse>(TRequest request)
        where TRequest : IRequest<TResponse>;
}