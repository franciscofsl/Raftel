using Raftel.Core.BaseTypes;

namespace Raftel.Infrastructure.Tests.DomainEvents;

public record SampleDomainEvent : DomainEvent
{
    public string EventData { get; set; }
}