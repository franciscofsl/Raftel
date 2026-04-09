using Raftel.Application.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests;

public class WideEventTests
{
    [Fact]
    public void Add_ShouldStorePropertyValue()
    {
        var wideEvent = new WideEvent();

        wideEvent.Add("user_id", "user_123");

        var properties = wideEvent.GetProperties();
        properties["user_id"].ShouldBe("user_123");
    }

    [Fact]
    public void Add_WhenCalledMultipleTimes_ShouldAccumulateProperties()
    {
        var wideEvent = new WideEvent();

        wideEvent.Add("method", "POST");
        wideEvent.Add("path", "/api/checkout");
        wideEvent.Add("user_id", "user_456");

        var properties = wideEvent.GetProperties();
        properties.Count.ShouldBe(3);
        properties["method"].ShouldBe("POST");
        properties["path"].ShouldBe("/api/checkout");
        properties["user_id"].ShouldBe("user_456");
    }

    [Fact]
    public void Add_WhenSameKeyIsUsedTwice_ShouldOverwriteValue()
    {
        var wideEvent = new WideEvent();

        wideEvent.Add("status_code", 200);
        wideEvent.Add("status_code", 500);

        var properties = wideEvent.GetProperties();
        properties["status_code"].ShouldBe(500);
    }

    [Fact]
    public void Add_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var wideEvent = new WideEvent();

        Should.Throw<ArgumentNullException>(() => wideEvent.Add(null, "value"));
    }

    [Fact]
    public void GetProperties_WhenNoPropertiesAdded_ShouldReturnEmptyDictionary()
    {
        var wideEvent = new WideEvent();

        var properties = wideEvent.GetProperties();

        properties.ShouldBeEmpty();
    }

    [Fact]
    public void GetProperties_ShouldReturnReadOnlyView()
    {
        var wideEvent = new WideEvent();
        wideEvent.Add("key", "value");

        var properties = wideEvent.GetProperties();

        properties.ShouldBeAssignableTo<IReadOnlyDictionary<string, object>>();
    }
}
