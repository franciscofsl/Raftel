using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Api.FunctionalTests.DemoApi.TestSupport;

/// <summary>
/// Renames a tenant - an ordinary command over a version-tracked aggregate, used to exercise
/// <c>If-Match</c> end to end.
/// </summary>
public sealed record RenameTenantCommand(Guid TenantId, string Name) : ICommand;

internal sealed class RenameTenantCommandHandler(ITenantsRepository tenantsRepository)
    : ICommandHandler<RenameTenantCommand>
{
    public async Task<Result> HandleAsync(RenameTenantCommand request, CancellationToken token = default)
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
