using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Raftel.Core.BaseTypes;

namespace Raftel.Data.Interceptors;

public sealed class ConvertDomainEventsToOutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;

        if (dbContext is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var outboxMessages = GetOutboxMessages(dbContext);
        dbContext.Set<OutboxMessage>().AddRange(outboxMessages);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static List<OutboxMessage> GetOutboxMessages(DbContext dbContext)
    {
        var entitiesWithEvents = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .ToList();

        var outboxMessages = entitiesWithEvents
            .SelectMany(entry => entry.Entity.DomainEvents)
            .Select(domainEvent => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOn = DateTime.UtcNow,
                Type = domainEvent.GetType().Name,
                Content = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNameCaseInsensitive = false,
                    Converters = { new JsonStringEnumConverter() }
                })
            })
            .ToList();

        foreach (var entityEntry in entitiesWithEvents)
        {
            entityEntry.Entity?.ClearDomainEvents();
        }

        return outboxMessages;
    }
}