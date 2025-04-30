using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Middlewares;

public class UnitOfWorkMiddleware<TRequest>(IUnitOfWork unitOfWork) : ICommandMiddleware<TRequest>
    where TRequest : ICommand
{
    public async Task<Result> HandleAsync(TRequest request, RequestHandlerDelegate<Result> next)
    {
        var response = await next();

        if (response is { IsSuccess: true })
        {
            await unitOfWork.CommitAsync();
        }

        return response;
    }
}