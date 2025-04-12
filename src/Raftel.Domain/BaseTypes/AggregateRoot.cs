namespace Raftel.Domain.BaseTypes;

/// <summary>
/// Represents the root of an aggregate in the domain model.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : TypedId<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The identifier of the aggregate root.</param>
    protected AggregateRoot(TId id) : base(id) { }
}