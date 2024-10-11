using Raftel.Core.Common.ValueObjects;
using Raftel.Shared.Results;

namespace Raftel.Core.CustomEntities.CustomFieldsTypes;

public sealed class DecimalCustomField : CustomFieldBase
{
    internal DecimalCustomField(string key, string name, bool isRequired) : base(key, name, isRequired)
    {
    }

    public override CustomFieldKind Kind => CustomFieldKind.Decimal;

    public Range<decimal> Range { get; set; }

    protected override Result ValueHasValidType(object value)
    {
        return value is null or decimal
            ? Result.Ok()
            : Result.Failure(CustomEntitiesErrors.CustomFieldValueNotOfConfiguredType);
    }

    protected override Result BeforeUpdateValidations(object value)
    {
        if (value is decimal decimalValue && Range.IsWithinRange(decimalValue))
        {
            return Result.Ok();
        }

        return Result.Failure(CustomEntitiesErrors.ValueNotInRange);
    }
}