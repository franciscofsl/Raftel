using System.Diagnostics.CodeAnalysis;
using Raftel.Core.BaseTypes;

namespace Raftel.Inkventory.Core.Customers;

public sealed class Customer : AggregateRoot<CustomerId>
{
    [ExcludeFromCodeCoverage]
    private Customer()
    {
        /* ORM Purpose */
    }

    public Customer(Name name, FirstLastName firstLastName) : base(CustomerId.New())
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FirstLastName = firstLastName ?? throw new ArgumentNullException(nameof(firstLastName));
        RaiseDomainEvent(new CustomerCreated(Id, name, firstLastName));
    }

    public Name Name { get; private set; }

    public FirstLastName FirstLastName { get; }

    public void Rename(Name name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }
}