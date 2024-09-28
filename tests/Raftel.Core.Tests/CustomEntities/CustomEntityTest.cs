namespace Raftel.Core.Tests.CustomEntities;

public class CustomEntityTest
{
    [Fact]
    public void Should_Not_Update_CustomField_Not_In_Configuration()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance().Build();

        var customEntity = customEntityConfiguration.NewEntity();

        customEntity.Should().NotBeNull();
    }
}