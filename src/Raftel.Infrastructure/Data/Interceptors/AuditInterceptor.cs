using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Raftel.Infrastructure.Data.Audit;
using System.Text.Json;

namespace Raftel.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor that captures entity changes and creates audit records.
/// </summary>
internal sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly AuditableEntitiesOptions _auditableEntitiesOptions;

    public AuditInterceptor(AuditableEntitiesOptions auditableEntitiesOptions)
    {
        _auditableEntitiesOptions = auditableEntitiesOptions;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureAuditEntries(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        var auditEntries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (!ShouldAuditEntity(entry.Entity.GetType()))
            {
                continue;
            }

            var auditEntry = CreateAuditEntry(entry);
            if (auditEntry != null)
            {
                auditEntries.Add(auditEntry);
            }
        }

        // Add audit entries to the context
        foreach (var auditEntry in auditEntries)
        {
            context.Set<AuditEntry>().Add(auditEntry);
        }
    }

    private bool ShouldAuditEntity(Type entityType)
    {
        return _auditableEntitiesOptions.IsAuditable(entityType);
    }

    private AuditEntry? CreateAuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var configuration = _auditableEntitiesOptions.GetConfiguration(entry.Entity.GetType());
        if (configuration == null)
        {
            return null;
        }

        var changeType = GetChangeType(entry, configuration);
        if (changeType == null)
        {
            return null;
        }

        var entityId = GetEntityId(entry);
        var entityName = entry.Entity.GetType().Name;

        var auditEntry = AuditEntry.Create(changeType, entityName, entityId);

        // Capture property changes for updates
        if (entry.State == EntityState.Modified && configuration.AuditUpdate)
        {
            CapturePropertyChanges(entry, auditEntry, configuration);
        }
        // Capture all properties for creation
        else if (entry.State == EntityState.Added && configuration.AuditCreation)
        {
            CaptureAllProperties(entry, auditEntry, configuration);
        }

        return auditEntry;
    }

    private string? GetChangeType(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, AuditEntityConfiguration configuration)
    {
        return entry.State switch
        {
            EntityState.Added when configuration.AuditCreation => AuditChangeType.Create,
            EntityState.Modified when configuration.AuditUpdate => AuditChangeType.Update,
            EntityState.Deleted when configuration.AuditDeletion => AuditChangeType.Delete,
            _ => null
        };
    }

    private string GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var keyProperties = entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .ToList();

        if (keyProperties.Count == 1)
        {
            return keyProperties[0].CurrentValue?.ToString() ?? string.Empty;
        }

        // For composite keys, create a JSON representation
        var keyValues = keyProperties.ToDictionary(
            p => p.Metadata.Name,
            p => p.CurrentValue);

        return JsonSerializer.Serialize(keyValues);
    }

    private void CapturePropertyChanges(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        AuditEntry auditEntry,
        AuditEntityConfiguration configuration)
    {
        foreach (var property in entry.Properties)
        {
            if (!configuration.ShouldAuditProperty(property.Metadata.Name))
            {
                continue;
            }

            if (!property.IsModified)
            {
                continue;
            }

            var oldValue = SerializeValue(property.OriginalValue);
            var newValue = SerializeValue(property.CurrentValue);

            if (oldValue != newValue)
            {
                auditEntry.AddPropertyChange(property.Metadata.Name, oldValue, newValue);
            }
        }
    }

    private void CaptureAllProperties(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        AuditEntry auditEntry,
        AuditEntityConfiguration configuration)
    {
        foreach (var property in entry.Properties)
        {
            if (!configuration.ShouldAuditProperty(property.Metadata.Name))
            {
                continue;
            }

            var newValue = SerializeValue(property.CurrentValue);
            auditEntry.AddPropertyChange(property.Metadata.Name, null, newValue);
        }
    }

    private string? SerializeValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is string stringValue)
        {
            return stringValue;
        }

        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch
        {
            return value.ToString();
        }
    }
}