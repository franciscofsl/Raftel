using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Abstractions.Auditing;
using Raftel.Infrastructure.Data.Auditing;

namespace Raftel.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor that captures entity-level changes (creations, updates and deletions) and
/// persists them as an <see cref="Raftel.Domain.Auditing.AuditLog"/> within the same
/// <c>SaveChanges</c> call that produced them.
/// </summary>
/// <remarks>
/// All EF Core-specific logic is delegated to <see cref="IChangeSnapshotExtractor"/>; aggregate
/// construction is delegated to <see cref="IAuditLogFactory"/>; persistence is delegated to
/// <see cref="IAuditStore"/>. This interceptor only orchestrates those independent services.
/// </remarks>
internal sealed class EntityChangesTrackerInterceptor : SaveChangesInterceptor
{
    private readonly IChangeSnapshotExtractor _extractor;
    private readonly IAuditLogFactory _factory;
    private readonly IAuditStore _store;
    private readonly IAuditLogScope _auditLogScope;
    private readonly ICurrentUser? _currentUser;
    private readonly TimeProvider _timeProvider;

    public EntityChangesTrackerInterceptor(
        IServiceProvider serviceProvider,
        IChangeSnapshotExtractor extractor,
        IAuditLogFactory factory,
        IAuditStore store,
        IAuditLogScope auditLogScope,
        TimeProvider timeProvider)
    {
        _extractor = extractor;
        _factory = factory;
        _store = store;
        _auditLogScope = auditLogScope;
        // ICurrentUser may not be available in some contexts (migrations, seeds, etc.)
        _currentUser = serviceProvider.GetService<ICurrentUser>();
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        TrackChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        TrackChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void TrackChanges(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var snapshots = _extractor.Extract(context);
        if (snapshots.Count == 0)
        {
            return;
        }

        var command = _auditLogScope.Command ?? "Unknown";
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var auditLog = _factory.Create(command, now, _currentUser?.UserId, _currentUser?.UserName, snapshots);

        if (auditLog is not null)
        {
            _store.Save(context, auditLog);
        }
    }
}
