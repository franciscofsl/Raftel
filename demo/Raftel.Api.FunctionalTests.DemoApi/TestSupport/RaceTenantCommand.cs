using Microsoft.EntityFrameworkCore;
using Raftel.Application.Commands;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Api.FunctionalTests.DemoApi.TestSupport;

/// <summary>
/// Loads a tenant, lets a second writer change it in between, then saves its own change - the
/// deterministic way to provoke a concurrency conflict from a single HTTP request.
/// </summary>
public sealed record RaceTenantCommand(Guid TenantId) : ICommand;

internal sealed class RaceTenantCommandHandler(
    TestingRaftelDbContext dbContext,
    IServiceScopeFactory scopeFactory) : ICommandHandler<RaceTenantCommand>
{
    public async Task<Result> HandleAsync(RaceTenantCommand request, CancellationToken token = default)
    {
        var tenantId = new TenantId(request.TenantId);
        var tenant = await dbContext.Set<Tenant>().SingleAsync(t => t.Id == tenantId, token);

        using (var otherScope = scopeFactory.CreateScope())
        {
            var otherContext = otherScope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
            var sameTenant = await otherContext.Set<Tenant>().SingleAsync(t => t.Id == tenantId, token);
            sameTenant.Description = $"Changed by the other writer {Guid.NewGuid():N}";
            await otherContext.SaveChangesAsync(token);
        }

        tenant.Name = $"Raced {Guid.NewGuid():N}"[..20];
        return Result.Success();
    }
}
