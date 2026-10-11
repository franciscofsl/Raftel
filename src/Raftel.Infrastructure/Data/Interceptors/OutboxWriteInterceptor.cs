using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Abstractions.Outbox;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Outbox;
using Raftel.Infrastructure.Outbox;

namespace Raftel.Infrastructure.Data.Interceptors;

/// <summary>
/// Stores the domain events raised by tracked aggregates as outbox messages in the same
/// <c>SaveChanges</c> as the state change that raised them, so the events commit or roll back together
/// with it. Delivery to the handlers happens later, in the outbox processor.
/// </summary>
/// <remarks>
/// Events without a registered handler are dropped, as they would be by in-process dispatch. If an event
/// cannot be serialized the save fails and nothing is persisted or cleared.
/// </remarks>
internal sealed class OutboxWriteInterceptor : SaveChangesInterceptor
{
    private readonly IOutboxSerializer _serializer;
    private readonly IDomainEventTypeRegistry _registry;
    private readonly ICurrentTenant? _currentTenant;
    private readonly ICorrelationContext? _correlationContext;

    public OutboxWriteInterceptor(IServiceProvider serviceProvider)
    {
        _registry = serviceProvider.GetService<IDomainEventTypeRegistry>()
                    ?? throw OutboxRegistrationException.MissingEventTypeRegistry();
        _serializer = serviceProvider.GetRequiredService<IOutboxSerializer>();
        _currentTenant = serviceProvider.GetService<ICurrentTenant>();
        _correlationContext = serviceProvider.GetService<ICorrelationContext>();
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StoreDomainEvents(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StoreDomainEvents(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StoreDomainEvents(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var aggregates = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var messages = aggregates
            .SelectMany(aggregate => aggregate.DomainEvents)
            .Where(domainEvent => _registry.TryGetName(domainEvent.GetType(), out _))
            .Select(ToMessage)
            .ToList();

        context.Set<OutboxMessage>().AddRange(messages);
        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());
    }

    private OutboxMessage ToMessage(IDomainEvent domainEvent)
    {
        var stored = _serializer.Serialize(domainEvent);

        return OutboxMessage.Create(
            stored.Type,
            stored.Content,
            domainEvent.OccurredOn,
            _currentTenant?.Id,
            _correlationContext?.CorrelationId);
    }
}
