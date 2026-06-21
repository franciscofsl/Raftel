namespace Raftel.Domain.Abstractions;

/// <summary>
/// Exposes the pending domain events of an aggregate without requiring its generic
/// <c>AggregateRoot&lt;TId&gt;</c> type, so infrastructure can collect and clear them via
/// <c>ChangeTracker.Entries&lt;IHasDomainEvents&gt;()</c>.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
