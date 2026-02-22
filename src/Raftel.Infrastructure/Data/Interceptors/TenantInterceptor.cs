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
        if (context == null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            var entityType = entry.Metadata;
            var tenantIdProperty = entityType.FindProperty(ShadowPropertyNames.TenantId);

            if (tenantIdProperty is null)
            {
                continue;
            }

            if (entry.State == EntityState.Added && _currentTenant?.Id != null)
            {
                entry.Property(ShadowPropertyNames.TenantId).CurrentValue = _currentTenant.Id;
            }
            else if (entry.State == EntityState.Modified)
            {
                var property = entry.Property(ShadowPropertyNames.TenantId);
                if (property.IsModified)
                {
                    property.CurrentValue = property.OriginalValue;
                }
            }
        }
    }
} 