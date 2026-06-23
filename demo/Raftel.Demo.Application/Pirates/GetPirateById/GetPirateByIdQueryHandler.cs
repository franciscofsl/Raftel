using Microsoft.Extensions.Localization;
using Raftel.Application.Queries;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Application.Pirates.GetPirateById;

internal sealed class GetPirateByIdQueryHandler : IQueryHandler<GetPirateByIdQuery, GetPirateByIdResponse>
{
    private readonly IStringLocalizer<PiratesModule> _localizer;

    public GetPirateByIdQueryHandler(IStringLocalizer<PiratesModule> localizer)
    {
        _localizer = localizer;
    }

    public async Task<Result<GetPirateByIdResponse>> HandleAsync(GetPirateByIdQuery request,
        CancellationToken token = default)
    {
        var mugiwara = MugiwaraCrew.All.FirstOrDefault(_ => _.Id == request.Id);

        if (mugiwara is null)
        {
            // Use localized error message
            var errorMessage = _localizer["PirateNotFound"].Value;
            return Result.Failure<GetPirateByIdResponse>(new Error("PirateNotFound", errorMessage));
        }

        return Result<GetPirateByIdResponse>.Success(new GetPirateByIdResponse
        {
            Bounty = mugiwara.Bounty,
            Id = mugiwara.Id,
            Name = mugiwara.Name
        });
    }
}