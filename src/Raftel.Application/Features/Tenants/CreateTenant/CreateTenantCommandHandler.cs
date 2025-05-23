using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;

namespace Raftel.Application.Features.Tenants.CreateTenant;

internal sealed class CreateTenantCommandHandler(ITenantsRepository tenantsRepository) 
    : ICommandHandler<CreateTenantCommand>
{
    public async Task<Result> HandleAsync(CreateTenantCommand request, CancellationToken token = default)
    {
        var tenantResult = Tenant.Create(request.Name, request.Code, request.Description);
        if (tenantResult.IsFailure)
        {
            return Result.Failure(tenantResult.Error);
        }

        if (await tenantsRepository.CodeIsUniqueAsync(request.Code, token) == false)
        {
            return Result.Failure(TenantErrors.DuplicatedCode);
        }

        await tenantsRepository.AddAsync(tenantResult.Value, token);
        return Result.Success();
    }
} 