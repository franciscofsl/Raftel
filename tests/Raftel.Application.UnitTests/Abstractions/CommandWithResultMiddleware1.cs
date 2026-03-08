using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Application.Middlewares;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.UnitTests.Abstractions;

public class CommandWithResultMiddleware1<TRequest, TResult>(ISpy spy)
    : ICommandMiddleware<TRequest, TResult>
    where TRequest : ICommand<TResult>
{
    public async Task<Result<TResult>> HandleAsync(TRequest request,
        RequestHandlerDelegate<Result<TResult>> next)
    {
        spy.Intercept("Hi CommandResult 1");
        var result = await next();
        spy.Intercept("By CommandResult 1");
        return result;
    }
}
