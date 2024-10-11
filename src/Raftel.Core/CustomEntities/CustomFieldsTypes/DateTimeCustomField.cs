using Raftel.Shared.Results;

namespace Raftel.Core.CustomEntities.CustomFieldsTypes;

public sealed class DateTimeCustomField : CustomFieldBase
{
    internal DateTimeCustomField(string key, string name, bool isRequired) : base(key, name, isRequired)
    {
    }

    public override CustomFieldKind Kind => CustomFieldKind.DateTime;

    protected override Result ValueHasValidType(object value)
    {
        return value is null or DateTime
            ? Result.Ok()
            : Result.Failure(CustomEntitiesErrors.CustomFieldValueNotOfConfiguredType);
    }

    internal override Result CheckDependentFieldValue(object dependencyValue, object dependantValue,
        EqualityKind equalityKind)
    {
        var startDate = dependencyValue as DateTime?;
        var endDate = dependantValue as DateTime?;

        if (startDate == null || endDate == null)
        {
            return Result.Ok();
        }

        if (Equals(equalityKind, EqualityKind.Equal) && startDate != endDate)
        {
            return Result.Failure(CustomEntitiesErrors.DatesShouldBeEquals);
        }

        if (Equals(equalityKind, EqualityKind.NotEqual) && startDate == endDate)
        {
            return Result.Failure(CustomEntitiesErrors.DatesShouldBeDifferent);
        }

        if (Equals(equalityKind, EqualityKind.GreaterThan) && endDate <= startDate)
        {
            return Result.Failure(CustomEntitiesErrors.DateMustBeGreaterThan);
        }
        if (Equals(equalityKind, EqualityKind.LessThan) && endDate >= startDate)
        {
            return Result.Failure(CustomEntitiesErrors.DateMustBeLessThan);
        }
        return Result.Ok();
    }
}