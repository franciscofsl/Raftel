using Raftel.Application.Queries;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Application.Pirates.GetPirateById;

internal sealed class GetPirateByIdQueryHandler : IQueryHandler<GetPirateByIdQuery, GetPirateByIdResponse>
{
    public async Task<Result<GetPirateByIdResponse>> HandleAsync(GetPirateByIdQuery request,
        CancellationToken token = default)
    {
        var mugiwara = MugiwaraCrew.All.FirstOrDefault(_ => _.Id == request.Id);

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