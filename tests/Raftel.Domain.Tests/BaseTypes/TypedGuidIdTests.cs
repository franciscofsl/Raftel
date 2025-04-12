using Shouldly;

namespace Raftel.Domain.Tests.BaseTypes;

public class TypedGuidIdTests
{
    [Fact]
    public void Create_ShouldGenerate_NonEmptyGuid()
    {
        var customerId = CustomerId.Create();

        customerId.ShouldNotBeNull();
        ((Guid)customerId).ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void EqualIds_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = new CustomerId(guid);
        var id2 = new CustomerId(guid);

        id1.ShouldBe(id2);
    }

    [Fact]
    public void DifferentIds_ShouldNotBeEqual()
    {
        var id1 = CustomerId.Create();
        var id2 = CustomerId.Create();

        id1.ShouldNotBe(id2);
    }
}