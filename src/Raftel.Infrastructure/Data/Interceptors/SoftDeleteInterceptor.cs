using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Raftel.Demo.Infrastructure.Data.Interceptors;

public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplySoftDelete(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted))
        {
            var entityType = entry.Metadata;

            var isDeletedProperty = entityType.FindProperty("IsDeleted");

            if (isDeletedProperty != null && isDeletedProperty.ClrType == typeof(bool))
            {
                // Establece IsDeleted = true (aunque sea shadow property)
                entry.Property("IsDeleted").CurrentValue = true;

                // Cambia el estado a Modified
                entry.State = EntityState.Modified;
            }
        }
    }
}