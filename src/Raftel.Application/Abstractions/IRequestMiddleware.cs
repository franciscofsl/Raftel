namespace Raftel.Application.Abstractions;

public interface IRequestMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next);
}