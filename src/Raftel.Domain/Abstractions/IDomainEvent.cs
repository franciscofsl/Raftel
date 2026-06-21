namespace Raftel.Domain.Abstractions;

/// <summary>
/// Marks an immutable domain event. Implement as a <c>sealed record</c>.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
