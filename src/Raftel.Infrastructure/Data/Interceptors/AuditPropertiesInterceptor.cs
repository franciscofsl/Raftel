using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Authentication;

namespace Raftel.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor that automatically applies audit properties (soft delete and user auditing) to entities.
/// </summary>
internal sealed class AuditPropertiesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser? _currentUser;
    private readonly TimeProvider _timeProvider;

    public AuditPropertiesInterceptor(IServiceProvider serviceProvider, TimeProvider timeProvider)
    {
        // ICurrentUser may not be available in some contexts (migrations, seeds, etc.)
        _currentUser = serviceProvider.GetService<ICurrentUser>();
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditProperties(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditProperties(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditProperties(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        var userId = _currentUser?.UserId;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var entityType = entry.Metadata;

            // Soft Delete
            if (entry.State == EntityState.Deleted)
            {
                var isDeletedProperty = entityType.FindProperty(ShadowPropertyNames.IsDeleted);

                if (isDeletedProperty is not null)
                {
                    entry.State = EntityState.Modified;
                    entry.Property(ShadowPropertyNames.IsDeleted).CurrentValue = true;
                }
            }

            // User Auditing - Creation
            if (entry.State == EntityState.Added)
            {
                if (HasProperty(entityType, ShadowPropertyNames.CreatorId))
                {
                    entry.Property(ShadowPropertyNames.CreatorId).CurrentValue = userId;
                }

                if (HasProperty(entityType, ShadowPropertyNames.CreationTime))
                {
                    entry.Property(ShadowPropertyNames.CreationTime).CurrentValue = now;
                }
            }

            // User Auditing - Modification
            if (entry.State == EntityState.Modified)
            {
                if (HasProperty(entityType, ShadowPropertyNames.LastModifierId))
                {
                    entry.Property(ShadowPropertyNames.LastModifierId).CurrentValue = userId;
                }

                if (HasProperty(entityType, ShadowPropertyNames.LastModificationTime))
                {
                    entry.Property(ShadowPropertyNames.LastModificationTime).CurrentValue = now;
                }
            }
        }
    }

    private static bool HasProperty(Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType, string propertyName)
    {
        return entityType.FindProperty(propertyName) is not null;
    }
}
