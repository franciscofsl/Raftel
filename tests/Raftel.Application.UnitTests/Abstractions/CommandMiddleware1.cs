using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Application.Middlewares;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.UnitTests.Abstractions;

public class CommandMiddleware1<TRequest>(ISpy spy) : ICommandMiddleware<TRequest> where TRequest : ICommand
{
    public async Task<Result> HandleAsync(TRequest request, RequestHandlerDelegate<Result> next,
        CancellationToken cancellationToken)
    {
        spy.Intercept("Hi Command 1");
        var result = await next(cancellationToken);
        spy.Intercept("By Command 1");
        return result;
    }
}