namespace Raftel.Domain.BaseTypes;

/// <summary>
/// Base class for all domain entities with a strongly-typed ID.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class Entity<TId> where TId : TypedId<Guid>
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    public TId Id { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    protected Entity(TId id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    protected Entity()
    {
        
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();
}