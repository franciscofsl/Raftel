﻿namespace Raftel.Core.CustomEntities;

[ExcludeFromCodeCoverage]
public static class CustomEntitiesErrors
{
    public static string CustomFieldIsRequired = "CustomEntities:CustomFields:RequiredFieldWithoutValue";

    public static string CustomFieldValueNotOfConfiguredType =
        "CustomEntities:CustomFields:CustomFieldValueNotOfConfiguredType";

    public static string CustomFieldNotSupportedByEntity =
        "CustomEntities:CustomFields:CustomFieldNotSupportedByEntity";

    public static string ValueNotInRange = "CustomEntities:CustomFields:ValueNotInRange";
}