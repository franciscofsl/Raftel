using System.Diagnostics.CodeAnalysis;

namespace Raftel.Core.BaseTypes;

public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    [ExcludeFromCodeCoverage]
    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }
}