using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Raftel.Application.Abstractions.Concurrency;
using Raftel.Application.Exceptions;

namespace Raftel.Infrastructure.Data.Interceptors;

/// <summary>
/// Enforces the version the caller expects (<see cref="IExpectedVersion"/>) by replacing the original
/// value of the concurrency token of the version-tracked entities about to be written, so the database
/// compares it atomically with the write instead of the application checking first.
/// </summary>
internal sealed class ExpectedVersionInterceptor(IExpectedVersion expectedVersion) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyExpectedVersion(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyExpectedVersion(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyExpectedVersion(DbContext context)
    {
        if (context is null || expectedVersion.Value is null)
        {
            return;
        }

        var entries = context.ChangeTracker.Entries()
            .Where(IsWrittenAndVersionTracked)
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        var version = expectedVersion.Consume()!;

        foreach (var entry in entries)
        {
            var token = entry.Property(ShadowPropertyNames.Version);

            if (!VersionCodec.TryDecode(version, token.Metadata.ClrType, out var raw))
            {
                throw new ConcurrencyConflictException(
                    entries.Select(e => e.Metadata.ClrType.Name).Distinct().ToList());
            }

            token.OriginalValue = raw;
        }
    }

    private static bool IsWrittenAndVersionTracked(EntityEntry entry) =>
        entry.State is EntityState.Modified or EntityState.Deleted &&
        entry.Metadata.FindProperty(ShadowPropertyNames.Version) is not null;
}
