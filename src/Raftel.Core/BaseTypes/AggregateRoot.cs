namespace Raftel.Core.BaseTypes;

public abstract class AggregateRoot<TKey> : WithDomainEvents
{
    public TKey Id { get; protected init; }
}