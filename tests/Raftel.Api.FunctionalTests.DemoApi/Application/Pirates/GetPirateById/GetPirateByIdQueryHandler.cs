using Raftel.Domain.Abstractions;
using Raftel.Tests.Common.Domain;

namespace Raftel.Api.FunctionalTests.DemoApi.Application.Pirates.GetPirateById;

internal sealed class GetPirateByIdQueryHandler : IQueryHandler<GetPirateByIdQuery, GetPirateByIdResponse>
{
    public async Task<Result<GetPirateByIdResponse>> HandleAsync(GetPirateByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        var mugiwara = Mugiwara.All.FirstOrDefault(_ => _.Id == request.Id);

        if (mugiwara is null)
        {
            return Result.Failure<GetPirateByIdResponse>(new Error("PirateNotFound", "Pirate not found"));
        }

        return Result<GetPirateByIdResponse>.Success(new GetPirateByIdResponse
        {
            Bounty = mugiwara.Bounty,
            Id = mugiwara.Id,
            Name = mugiwara.Name
        });
    }
}