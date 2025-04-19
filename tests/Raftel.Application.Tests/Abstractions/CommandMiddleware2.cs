using Raftel.Application.Abstractions;

namespace Raftel.Application.Tests.Abstractions;

public class CommandMiddleware2<TRequest, TResponse> : IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _log;
    public CommandMiddleware2(List<string> log) => _log = log;

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        _log.Add("Command2");
        return await next();
    }
}