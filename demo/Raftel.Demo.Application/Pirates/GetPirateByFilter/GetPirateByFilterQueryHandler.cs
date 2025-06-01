using Raftel.Application.Queries;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Abstractions;
using Raftel.Shared.Extensions;

namespace Raftel.Demo.Application.Pirates.GetPirateByFilter;

internal sealed class GetPirateByFilterQueryHandler(IPirateRepository repository)
    : IQueryHandler<GetPirateByFilterQuery, GetPirateByFilterResponse>
{
    public async Task<Result<GetPirateByFilterResponse>> HandleAsync(GetPirateByFilterQuery request,
        CancellationToken token = default)
    {
        var pirates = await repository.ListAllAsync(cancellationToken: token);
        var result = pirates
            .WhereIf(!string.IsNullOrEmpty(request.Name),
                _ => ((string)_.Name).Contains(request.Name, StringComparison.OrdinalIgnoreCase))
            .WhereIf(request.MaxBounty.HasValue, _ => _.Bounty <= request.MaxBounty)
            .Select(_ => new PirateInfo(_.Name, _.Bounty));

        return Result.Success(new GetPirateByFilterResponse
        {
            Pirates = result.ToList()
        });
    }
}