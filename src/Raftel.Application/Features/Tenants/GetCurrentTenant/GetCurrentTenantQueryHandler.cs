using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Features.Tenants.GetCurrentTenant;

internal sealed class GetCurrentTenantQueryHandler(ICurrentTenant currentTenant)
    : IQueryHandler<GetCurrentTenantQuery, GetCurrentTenantResponse>
{
    public Task<Result<GetCurrentTenantResponse>> HandleAsync(GetCurrentTenantQuery request,
        CancellationToken token = default)
    {
        var response = new GetCurrentTenantResponse
        {
            TenantId = currentTenant.Id,
            IsMultiTenant = currentTenant.Id.HasValue
        };

        return Task.FromResult<Result<GetCurrentTenantResponse>>(response);
    }
}