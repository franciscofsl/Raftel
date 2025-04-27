using Raftel.Domain.Abstractions;
using Raftel.Shared.Extensions;
using Raftel.Tests.Common.Domain;

namespace Raftel.Api.Integration.Tests.Api.Application.Pirates.GetPirateByFilter;

internal sealed class GetPirateByFilterQueryHandler : IQueryHandler<GetPirateByFilterQuery, GetPirateByFilterResponse>
{
    public async Task<Result<GetPirateByFilterResponse>> HandleAsync(GetPirateByFilterQuery request,
        CancellationToken token = default)
    {
        var pirates = Mugiwara.All
            .WhereIf(!string.IsNullOrEmpty(request.Name),
                _ => ((string)_.Name).Contains(request.Name, StringComparison.OrdinalIgnoreCase))
            .WhereIf(request.MaxBounty.HasValue, _ => _.Bounty <= request.MaxBounty)
            .Select(_ => new PirateInfo(_.Name, _.Bounty));

        return Result.Success(new GetPirateByFilterResponse
        {
            Pirates = pirates.ToList()
        });
    }
}