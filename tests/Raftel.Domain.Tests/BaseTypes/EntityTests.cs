using Shouldly;

namespace Raftel.Domain.Tests.BaseTypes;

public class EntityTests
{
    [Fact]
    public void Entities_WithSameId_ShouldBeEqual()
    {
        var id = CustomerId.Create();
        var customer1 = new Customer(id, "Nami");
        var customer2 = new Customer(id, "Nami");

        customer1.ShouldBe(customer2);
    }

    [Fact]
    public void Entities_WithDifferentIds_ShouldNotBeEqual()
    {
        var customer1 = new Customer(CustomerId.Create(), "Zoro");
        var customer2 = new Customer(CustomerId.Create(), "Zoro");

        customer1.ShouldNotBe(customer2);
    }

    [Fact]
    public void Entity_HasValidId()
    {
        var customer = new Customer(CustomerId.Create(), "Luffy");

        customer.Id.ShouldNotBeNull();
        ((Guid)customer.Id).ShouldNotBe(Guid.Empty);
    }
}