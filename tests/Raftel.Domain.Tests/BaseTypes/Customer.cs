using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Tests.BaseTypes;

public class Customer : AggregateRoot<CustomerId>
{
    public string Name { get; private set; }

    public Customer(CustomerId id, string name) : base(id)
    {
        Name = name;
    }
}