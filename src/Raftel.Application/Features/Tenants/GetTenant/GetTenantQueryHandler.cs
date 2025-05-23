using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Application.Features.Tenants.GetTenant;

internal sealed class GetTenantQueryHandler(ITenantsRepository tenantsRepository)
    : IQueryHandler<GetTenantQuery, GetTenantResponse>
{
    public async Task<Result<GetTenantResponse>> HandleAsync(GetTenantQuery request, CancellationToken token = default)
    {
        var tenant = await tenantsRepository.GetByIdAsync(new TenantId(request.Id), token);
        
        if (tenant is null)
        {
            return Result.Failure<GetTenantResponse>(new Error("Tenant.NotFound", "Tenant not found"));
        }

        return new GetTenantResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            Description = tenant.Description
        };
    }
} 