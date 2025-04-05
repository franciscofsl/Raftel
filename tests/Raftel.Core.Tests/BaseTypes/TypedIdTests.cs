using Raftel.Core.BaseTypes;
using Shouldly;

namespace Raftel.Core.Tests.BaseTypes;

public class TypedIdTests
{
    private record GuidTypedId : TypedId<Guid>
    {
        public GuidTypedId(Guid value) : base(value)
        {
        }
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenValueIsEmptyGuid()
    {
        Should.Throw<ArgumentException>(() => new GuidTypedId(Guid.Empty));
    }

    [Fact]
    public void ImplicitOperator_ShouldReturnValue()
    {
        var guidValue = Guid.NewGuid();
        var id = new GuidTypedId(guidValue);

        Guid result = id;

        result.ShouldBe(guidValue);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenIdsAreEqual()
    {
        var guidValue = Guid.NewGuid();
        var id1 = new GuidTypedId(guidValue);
        var id2 = new GuidTypedId(guidValue);

        var result = id1.Equals(id2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenIdsAreNotEqual()
    {
        var id1 = new GuidTypedId(Guid.NewGuid());
        var id2 = new GuidTypedId(Guid.NewGuid());

        var result = id1.Equals(id2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameValue_WhenIdsAreEqual()
    {
        var guidValue = Guid.NewGuid();
        var id1 = new GuidTypedId(guidValue);
        var id2 = new GuidTypedId(guidValue);

        var hashCode1 = id1.GetHashCode();
        var hashCode2 = id2.GetHashCode();

        hashCode1.ShouldBe(hashCode2);
    }

    [Fact]
    public void GetHashCode_ShouldReturnDifferentValues_WhenIdsAreNotEqual()
    {
        var id1 = new GuidTypedId(Guid.NewGuid());
        var id2 = new GuidTypedId(Guid.NewGuid());

        var hashCode1 = id1.GetHashCode();
        var hashCode2 = id2.GetHashCode();

        hashCode1.ShouldNotBe(hashCode2);
    }
}