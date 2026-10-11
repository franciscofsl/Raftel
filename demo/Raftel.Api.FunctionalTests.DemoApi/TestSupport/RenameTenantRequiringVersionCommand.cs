using Raftel.Application.Authorization;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Api.FunctionalTests.DemoApi.TestSupport;

/// <summary>
/// Same as <see cref="RenameTenantCommand"/>, but blind writes are forbidden: the caller must send
/// the version it read in <c>If-Match</c>.
/// </summary>
[RequiresExpectedVersion]
public sealed record RenameTenantRequiringVersionCommand(Guid TenantId, string Name) : ICommand;

internal sealed class RenameTenantRequiringVersionCommandHandler(ITenantsRepository tenantsRepository)
    : ICommandHandler<RenameTenantRequiringVersionCommand>
{
    public async Task<Result> HandleAsync(RenameTenantRequiringVersionCommand request,
        CancellationToken token = default)
    {
        var tenant = await tenantsRepository.GetByIdAsync(new TenantId(request.TenantId), token);

        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant not found"));
        }

        tenant.Name = request.Name;
        tenantsRepository.Update(tenant);
        return Result.Success();
    }
}
