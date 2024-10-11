namespace Raftel.Core.CustomEntities;

[ExcludeFromCodeCoverage]
public static class CustomEntitiesErrors
{
    public static string CustomFieldIsRequired = "CustomEntities:CustomFields:RequiredFieldWithoutValue";

    public static string CustomFieldValueNotOfConfiguredType =
        "CustomEntities:CustomFields:CustomFieldValueNotOfConfiguredType";

    public static string CustomFieldNotSupportedByEntity =
        "CustomEntities:CustomFields:CustomFieldNotSupportedByEntity";

    public static string ValueNotInRange = "CustomEntities:CustomFields:ValueNotInRange";

    public static string FieldAlreadyHaveThisDependency = "CustomEntities:CustomFields:FieldAlreadyHaveThisDependency";
    public static string DependencyNotHasValue = "CustomEntities:CustomFields:DependencyNotHasValue";
    public static string DateMustBeGreaterThan = "CustomEntities:CustomFields:InvalidDateRange";
    public static string DatesShouldBeEquals = "CustomEntities:CustomFields:DatesShouldBeEquals";
    public static string DatesShouldBeDifferent = "CustomEntities:CustomFields:DatesShouldBeDifferent";
    public static string DateMustBeGreaterOrEqualThan = "CustomEntities:CustomFields:DateMustBeGreaterOrEqualThan";
    public static string DateMustBeLessThan = "CustomEntities:CustomFields:DateMustBeLessThan";
    public static string DateMustBeLessOrEqualThan = "CustomEntities:CustomFields:DateMustBeLessOrEqualThan";
}