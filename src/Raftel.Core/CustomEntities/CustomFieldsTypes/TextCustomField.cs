using Raftel.Shared.Results;

namespace Raftel.Core.CustomEntities.CustomFieldsTypes;

public sealed class TextCustomField : CustomFieldBase
{
    internal TextCustomField(string key, string name, bool isRequired) : base(key, name, isRequired)
    {
    }

    public override CustomFieldKind Kind => CustomFieldKind.Text;

    protected override Result ValueHasValidType(object value)
    {
        return value is null or string
            ? Result.Ok()
            : Result.Failure(CustomEntitiesErrors.CustomFieldValueNotOfConfiguredType);
    }

    internal override Result CheckDependentFieldValue(object dependencyValue, object dependantValue,
        EqualityKind equalityKind)
    {
        var text1 = dependencyValue as string;
        var text2 = dependantValue as string;

        if (text1 == null || text2 == null)
        {
            return Result.Ok();
        }

        if (Equals(equalityKind, EqualityKind.Equal) && text1 != text2)
        {
            return Result.Failure(CustomEntitiesErrors.TextShouldBeEqual);
        }

        if (Equals(equalityKind, EqualityKind.NotEqual) && text1 == text2)
        {
            return Result.Failure(CustomEntitiesErrors.TextShouldBeDifferent);
        }
        
        if (Equals(equalityKind, EqualityKind.Contains) && !text2.Contains(text1))
        {
            return Result.Failure(CustomEntitiesErrors.TextMustContain);
        }

        if (Equals(equalityKind, EqualityKind.DoesNotContain) && text2.Contains(text1))
        {
            return Result.Failure(CustomEntitiesErrors.TextMustNotContain);
        }

        if (Equals(equalityKind, EqualityKind.StartsWith) && !text2.StartsWith(text1))
        {
            return Result.Failure(CustomEntitiesErrors.TextMustStartWith);
        }

        if (Equals(equalityKind, EqualityKind.EndsWith) && !text2.EndsWith(text1))
        {
            return Result.Failure(CustomEntitiesErrors.TextMustEndWith);
        }

        return Result.Ok();
    }
}