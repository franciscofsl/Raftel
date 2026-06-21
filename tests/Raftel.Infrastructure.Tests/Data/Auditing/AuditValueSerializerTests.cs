using Raftel.Infrastructure.Data.Auditing;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data.Auditing;

public class AuditValueSerializerTests
{
    private readonly AuditValueSerializer _serializer = new();

    [Fact]
    public void Serialize_WithNull_ShouldReturnNull()
    {
        _serializer.Serialize(null).ShouldBeNull();
    }

    [Fact]
    public void Serialize_WithString_ShouldReturnSameString()
    {
        _serializer.Serialize("Luffy").ShouldBe("Luffy");
    }

    [Fact]
    public void Serialize_WithInt_ShouldReturnInvariantStringRepresentation()
    {
        _serializer.Serialize(150_000_000).ShouldBe("150000000");
    }

    [Fact]
    public void Serialize_WithBool_ShouldReturnInvariantStringRepresentation()
    {
        _serializer.Serialize(true).ShouldBe("True");
    }

    [Fact]
    public void Serialize_WithGuid_ShouldReturnGuidString()
    {
        var guid = Guid.NewGuid();

        _serializer.Serialize(guid).ShouldBe(guid.ToString());
    }

    [Fact]
    public void Serialize_WithEnum_ShouldReturnEnumName()
    {
        _serializer.Serialize(SampleEnum.Second).ShouldBe("Second");
    }

    [Fact]
    public void Serialize_WithDateTime_ShouldReturnRoundTripFormat()
    {
        var dateTime = new DateTime(2026, 1, 1, 10, 30, 0, DateTimeKind.Utc);

        _serializer.Serialize(dateTime).ShouldBe(dateTime.ToString("O"));
    }

    [Fact]
    public void Serialize_WithValueObjectOverridingToString_ShouldReturnOverriddenRepresentation()
    {
        var email = new SampleEmail("luffy@strawhat.crew");

        _serializer.Serialize(email).ShouldBe("luffy@strawhat.crew");
    }

    [Fact]
    public void Serialize_WithRecordWithoutCustomToString_ShouldReturnCompilerGeneratedRepresentation()
    {
        var address = new SampleAddress("Sunny Street", "Grand Line");

        _serializer.Serialize(address).ShouldBe(address.ToString());
        _serializer.Serialize(address).ShouldContain("Sunny Street");
        _serializer.Serialize(address).ShouldContain("Grand Line");
    }

    [Fact]
    public void Serialize_WithNestedValueObject_ShouldRecursivelyIncludeInnerValue()
    {
        var nested = new SampleOuter(new SampleAddress("Sunny Street", "Grand Line"));

        var result = _serializer.Serialize(nested);

        result.ShouldContain("Sunny Street");
        result.ShouldContain("Grand Line");
    }

    private enum SampleEnum
    {
        First,
        Second
    }

    private sealed record SampleEmail(string Value)
    {
        public override string ToString() => Value;
    }

    private sealed record SampleAddress(string Street, string City);

    private sealed record SampleOuter(SampleAddress Address);
}
