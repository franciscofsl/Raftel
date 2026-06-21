using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Domain.Tests.BaseTypes;

public class AggregateRootTests
{
    [Fact]
    public void RaiseDomainEvent_ShouldRegisterEvent_WhenBusinessMethodIsCalled()
    {
        var customer = new Customer(CustomerId.Create(), "Nami");

        customer.Rename("Nico Robin");

        var domainEvent = customer.DomainEvents.ShouldHaveSingleItem();
        domainEvent.ShouldBeOfType<CustomerRenamed>();
    }

    [Fact]
    public void DomainEvents_ShouldBeReadOnly()
    {
        var customer = new Customer(CustomerId.Create(), "Nami");
        customer.Rename("Nico Robin");

        var events = (ICollection<IDomainEvent>)customer.DomainEvents;

        Should.Throw<NotSupportedException>(() => events.Add(new CustomerRenamed(customer.Id, "Anyone")));
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyTheCollection()
    {
        var customer = new Customer(CustomerId.Create(), "Nami");
        customer.Rename("Nico Robin");

        customer.ClearDomainEvents();

        customer.DomainEvents.ShouldBeEmpty();
    }
}
