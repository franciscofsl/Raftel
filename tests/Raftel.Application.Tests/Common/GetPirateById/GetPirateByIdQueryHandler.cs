using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Tests.Common.GetPirateById;
internal sealed class GetPirateByIdQueryHandler : IQueryHandler<GetPirateByIdQuery, GetPirateByIdResponse>
{
    public Task<Result<GetPirateByIdResponse>> HandleAsync(GetPirateByIdQuery request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}