using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions.Middlewares;

public class UnitOfWorkMiddleware<TRequest, TResponse> : IRequestMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkMiddleware(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        var response = await next();

        if (response is Result { IsSuccess: true })
        {
            await _unitOfWork.CommitAsync();
        }

        return response;
    }
}