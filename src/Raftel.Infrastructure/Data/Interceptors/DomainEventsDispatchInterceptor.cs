using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Domain.Abstractions;

namespace Raftel.Infrastructure.Data.Interceptors;

/// <summary>
/// Dispatches the domain events raised by tracked aggregates after a <c>SaveChanges</c> call
/// commits successfully, so events are never published for a transaction that didn't persist.
/// </summary>
internal sealed class DomainEventsDispatchInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventsDispatcher? _dispatcher;

    public DomainEventsDispatchInterceptor(IServiceProvider serviceProvider)
    {
        _dispatcher = serviceProvider.GetService<IDomainEventsDispatcher>();
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchDomainEventsAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null || _dispatcher is null)
        {
            return;
        }

        var aggregatesWithEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregatesWithEvents)
        {
            var events = aggregate.DomainEvents.ToArray();
            aggregate.ClearDomainEvents();

            await _dispatcher.DispatchAsync(events, cancellationToken);
        }
    }
}
