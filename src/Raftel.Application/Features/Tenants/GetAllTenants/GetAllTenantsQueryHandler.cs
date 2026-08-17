using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Specifications;

namespace Raftel.Application.Features.Tenants.GetAllTenants;

internal sealed class GetAllTenantsQueryHandler(ITenantsRepository tenantsRepository, PaginationOptions paginationOptions)
    : IQueryHandler<GetAllTenantsQuery, PagedResult<GetAllTenantsResponse>>
{
    private static readonly SortMap<Tenant> SortMap = new SortMap<Tenant>()
        .Allow("name", tenant => tenant.Name)
        .Allow("code", tenant => tenant.Code);

    public async Task<Result<PagedResult<GetAllTenantsResponse>>> HandleAsync(GetAllTenantsQuery request,
        CancellationToken token = default)
    {
        var pageResult = PageRequest.Create(request.Page ?? 1, request.PageSize ?? paginationOptions.DefaultPageSize);
        if (pageResult.IsFailure)
        {
            return Result.Failure<PagedResult<GetAllTenantsResponse>>(pageResult.Error);
        }

        var sortRequestsResult = SortRequest.Parse(request.Sort);
        if (sortRequestsResult.IsFailure)
        {
            return Result.Failure<PagedResult<GetAllTenantsResponse>>(sortRequestsResult.Error);
        }

        var sortSelectorsResult = SortMap.ResolveAll(sortRequestsResult.Value);
        if (sortSelectorsResult.IsFailure)
        {
            return Result.Failure<PagedResult<GetAllTenantsResponse>>(sortSelectorsResult.Error);
        }

        var tenants = await tenantsRepository.ListPagedAsync(pageResult.Value, sort: sortSelectorsResult.Value,
            cancellationToken: token);

        return tenants.Map(tenant => new GetAllTenantsResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            Description = tenant.Description
        });
    }
}