namespace Raftel.Core.BaseTypes;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}