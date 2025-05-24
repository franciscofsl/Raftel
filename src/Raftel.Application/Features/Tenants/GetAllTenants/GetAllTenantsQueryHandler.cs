using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;

namespace Raftel.Application.Features.Tenants.GetAllTenants;

internal sealed class GetAllTenantsQueryHandler(ITenantsRepository tenantsRepository)
    : IQueryHandler<GetAllTenantsQuery, List<GetAllTenantsResponse>>
{
    public async Task<Result<List<GetAllTenantsResponse>>> HandleAsync(GetAllTenantsQuery request,
        CancellationToken token = default)
    {
        var tenants = await tenantsRepository.ListAllAsync(token);

        var response = tenants
            .Select(tenant => new GetAllTenantsResponse
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Code = tenant.Code,
                Description = tenant.Description
            })
            .ToList();

        return response;
    }
}