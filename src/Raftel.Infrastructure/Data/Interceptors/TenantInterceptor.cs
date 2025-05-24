using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Raftel.Application.Abstractions.Multitenancy;

namespace Raftel.Infrastructure.Data.Interceptors;

internal sealed class TenantInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenant _currentTenant;

    public TenantInterceptor(ICurrentTenant currentTenant)
    {
        _currentTenant = currentTenant;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTenantId(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTenantId(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTenantId(DbContext? context)
    {
        if (context == null || _currentTenant?.Id == null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            var entityType = entry.Metadata;
            var tenantIdProperty = entityType.FindProperty(ShadowPropertyNames.TenantId);

            if (tenantIdProperty is null)
            {
                continue;
            }

            entry.Property(ShadowPropertyNames.TenantId).CurrentValue = _currentTenant.Id;
        }
    }
} 