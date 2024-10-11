using Raftel.Core.Common.ValueObjects;
using Raftel.Shared.Results;

namespace Raftel.Core.CustomEntities.CustomFieldsTypes;

public sealed class IntCustomField : CustomFieldBase
{
    internal IntCustomField(string key, string name, bool isRequired) : base(key, name, isRequired)
    {
    }

    public override CustomFieldKind Kind => CustomFieldKind.Integer;

    public Range<int> Range { get; set; }

    protected override Result ValueHasValidType(object value)
    {
        return value is null or int
            ? Result.Ok()
            : Result.Failure(CustomEntitiesErrors.CustomFieldValueNotOfConfiguredType);
    }

    protected override Result BeforeUpdateValidations(object value)
    {
        if (value is int intValue && Range.IsWithinRange(intValue))
        {
            return Result.Ok();
        }

        return Result.Failure(CustomEntitiesErrors.ValueNotInRange);
    }
}