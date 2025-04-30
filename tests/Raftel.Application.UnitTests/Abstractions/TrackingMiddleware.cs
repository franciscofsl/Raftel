using Raftel.Application.Abstractions;

namespace Raftel.Application.UnitTests.Abstractions;

public class TrackingMiddleware<TRequest, TResponse> : IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _executionLog;
    private readonly string _name;

    public TrackingMiddleware(List<string> log, string name)
    {
        _executionLog = log;
        _name = name;
    }

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        _executionLog.Add(_name);
        return await next();
    }
}