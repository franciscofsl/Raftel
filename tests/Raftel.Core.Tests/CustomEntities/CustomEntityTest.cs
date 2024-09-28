using Raftel.Core.Common.ValueObjects;
using Raftel.Core.CustomEntities;
using Raftel.Core.CustomEntities.CustomFieldsTypes;

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

    [Fact]
    public void CustomEntity_ShouldSave_RequiredTextValue()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField = customEntityConfiguration.AddCustomField("Name", "Person Name", CustomFieldKind.Text, true);

        var customEntity = customEntityConfiguration.NewEntity();

        customEntity.UpdateField(customField, "Monkey D. Luffy");

        customEntity.ValueOf(customField).Should().Be("Monkey D. Luffy");
    }

    [Fact]
    public void CustomEntity_Should_Not_Update_When_RequiredTextValue_Is_Null()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField = customEntityConfiguration.AddCustomField("Name", "Person Name", CustomFieldKind.Text, true);

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, null);

        result.Error.Code.Should().Be(CustomEntitiesErrors.CustomFieldIsRequired);
    }

    [Fact]
    public void CustomEntity_ShouldSave_RequiredIntValue()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField = customEntityConfiguration.AddCustomField("Age", "Person Age", CustomFieldKind.Integer, true);

        var customEntity = customEntityConfiguration.NewEntity();

        customEntity.UpdateField(customField, 30);

        customEntity.ValueOf(customField).Should().Be(30);
    }

    [Fact]
    public void CustomEntity_Should_Not_Update_When_RequiredIntValue_Is_Null()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField = customEntityConfiguration.AddCustomField("Age", "Person Age", CustomFieldKind.Integer, true);

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, null);

        result.Error.Code.Should().Be(CustomEntitiesErrors.CustomFieldIsRequired);
    }

    [Fact]
    public void CustomEntity_ShouldSave_RequiredDecimalValue()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField =
            customEntityConfiguration.AddCustomField("Salary", "Person Salary", CustomFieldKind.Decimal, true);

        var customEntity = customEntityConfiguration.NewEntity();

        customEntity.UpdateField(customField, 1000.50m);

        customEntity.ValueOf(customField).Should().Be(1000.50m);
    }

    [Fact]
    public void CustomEntity_Should_Not_Update_When_RequiredDecimalValue_Is_Null()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField =
            customEntityConfiguration.AddCustomField("Salary", "Person Salary", CustomFieldKind.Decimal, true);

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, null);

        result.Error.Code.Should().Be(CustomEntitiesErrors.CustomFieldIsRequired);
    }

    [Fact]
    public void CustomEntity_ShouldSave_RequiredBooleanValue()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField =
            customEntityConfiguration.AddCustomField("IsActive", "Is Active", CustomFieldKind.Boolean, true);

        var customEntity = customEntityConfiguration.NewEntity();

        customEntity.UpdateField(customField, true);

        customEntity.ValueOf(customField).Should().Be(true);
    }

    [Fact]
    public void CustomEntity_Should_Not_Update_When_RequiredBooleanValue_Is_Null()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField =
            customEntityConfiguration.AddCustomField("IsActive", "Is Active", CustomFieldKind.Boolean, true);

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, null);

        result.Error.Code.Should().Be(CustomEntitiesErrors.CustomFieldIsRequired);
    }

    [Fact]
    public void CustomEntity_ShouldSave_RequiredDateValue()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField =
            customEntityConfiguration.AddCustomField("BirthDate", "Birth Date", CustomFieldKind.Date, true);

        var customEntity = customEntityConfiguration.NewEntity();

        var dateValue = new DateOnly(1990, 1, 1);
        customEntity.UpdateField(customField, dateValue);

        customEntity.ValueOf(customField).Should().Be(dateValue);
    }

    [Fact]
    public void CustomEntity_Should_Not_Update_When_RequiredDateValue_Is_Null()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField =
            customEntityConfiguration.AddCustomField("BirthDate", "Birth Date", CustomFieldKind.Date, true);

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, null);

        result.Error.Code.Should().Be(CustomEntitiesErrors.CustomFieldIsRequired);
    }

    [Fact]
    public void CustomEntity_ShouldSave_RequiredTimeValue()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField =
            customEntityConfiguration.AddCustomField("MeetingTime", "Meeting Time", CustomFieldKind.Time, true);

        var customEntity = customEntityConfiguration.NewEntity();

        var timeValue = new TimeOnly(14, 30);
        customEntity.UpdateField(customField, timeValue);

        customEntity.ValueOf(customField).Should().Be(timeValue);
    }

    [Fact]
    public void CustomEntity_Should_Not_Update_When_RequiredTimeValue_Is_Null()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField =
            customEntityConfiguration.AddCustomField("MeetingTime", "Meeting Time", CustomFieldKind.Time, true);

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, null);

        result.Error.Code.Should().Be(CustomEntitiesErrors.CustomFieldIsRequired);
    }

    [Fact]
    public void CustomEntity_ShouldSave_RequiredDateTimeValue()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField =
            customEntityConfiguration.AddCustomField("EventDateTime", "Event DateTime", CustomFieldKind.DateTime, true);

        var customEntity = customEntityConfiguration.NewEntity();

        var dateTimeValue = new DateTime(2024, 1, 1, 10, 30, 0);
        customEntity.UpdateField(customField, dateTimeValue);

        customEntity.ValueOf(customField).Should().Be(dateTimeValue);
    }

    [Fact]
    public void CustomEntity_Should_Not_Update_When_RequiredDateTimeValue_Is_Null()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance()
            .Build();

        var customField =
            customEntityConfiguration.AddCustomField("EventDateTime", "Event DateTime", CustomFieldKind.DateTime, true);

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, null);

        result.Error.Code.Should().Be(CustomEntitiesErrors.CustomFieldIsRequired);
    }

    [Fact]
    public void UpdateField_ShouldNotUpdateIntegerValue_IfIsLowerOutsideOfRange()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance().Build();

        var customField = customEntityConfiguration.AddCustomField("Age", "Person Age", CustomFieldKind.Integer);

        if (customField is IntCustomField intCustomField)
        {
            intCustomField.Range = new Range<int>(2, 5);
        }

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, 1);

        result.Error.Code.Should().Be(CustomEntitiesErrors.ValueNotInRange);
    }

    [Fact]
    public void UpdateField_ShouldNotUpdateIntegerValue_IfIsGreaterOutsideOfRange()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance().Build();

        var customField = customEntityConfiguration.AddCustomField("Age", "Person Age", CustomFieldKind.Integer);

        if (customField is IntCustomField intCustomField)
        {
            intCustomField.Range = new Range<int>(2, 5);
        }

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, 6);

        result.Error.Code.Should().Be(CustomEntitiesErrors.ValueNotInRange);
    }

    [Fact]
    public void UpdateField_ShouldUpdateIntegerValue_IfIsWithinRange()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance().Build();

        var customField = customEntityConfiguration.AddCustomField("Age", "Person Age", CustomFieldKind.Integer);

        if (customField is IntCustomField intCustomField)
        {
            intCustomField.Range = new Range<int>(2, 5);
        }

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, 3);
        result.Success.Should().BeTrue();

        customEntity.ValueOf(customField).Should().Be(3);
    }

    [Fact]
    public void UpdateField_ShouldNotUpdateDecimalValue_IfIsLowerOutsideOfRange()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance().Build();

        var customField = customEntityConfiguration.AddCustomField("Age", "Person Age", CustomFieldKind.Decimal);

        if (customField is DecimalCustomField decimalCustomField)
        {
            decimalCustomField.Range = new Range<decimal>(2, 5);
        }

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, 1m);

        result.Error.Code.Should().Be(CustomEntitiesErrors.ValueNotInRange);
    }

    [Fact]
    public void UpdateField_ShouldNotUpdateDecimalValue_IfIsGreaterOutsideOfRange()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance().Build();

        var customField = customEntityConfiguration.AddCustomField("Age", "Person Age", CustomFieldKind.Decimal);

        if (customField is DecimalCustomField decimalCustomField)
        {
            decimalCustomField.Range = new Range<decimal>(2, 5);
        }

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, 6m);

        result.Error.Code.Should().Be(CustomEntitiesErrors.ValueNotInRange);
    }

    [Fact]
    public void UpdateField_ShouldUpdateDecimalValue_IfIsWithinRange()
    {
        var customEntityConfiguration = CustomEntityConfigurationBuilder.Instance().Build();

        var customField = customEntityConfiguration.AddCustomField("Age", "Person Age", CustomFieldKind.Decimal);

        if (customField is DecimalCustomField decimalCustomField)
        {
            decimalCustomField.Range = new Range<decimal>(1, 5);
        }

        var customEntity = customEntityConfiguration.NewEntity();

        var result = customEntity.UpdateField(customField, 3m);
        result.Success.Should().BeTrue();

        customEntity.ValueOf(customField).Should().Be(3m);
    }
}