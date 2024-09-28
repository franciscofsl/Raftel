using Raftel.Shared.Results;

namespace Raftel.Core.CustomEntities.CustomFieldsTypes;

public sealed class DateCustomField : CustomFieldBase
{
    internal DateCustomField(string key, string name, bool isRequired) : base(key, name, isRequired)
    {
    }

    public override CustomFieldKind Kind => CustomFieldKind.Date;

    protected override Result ValueHasValidType(object value)
    {
        return value is null or DateOnly
            ? Result.Ok()
            : Result.Failure(CustomEntitiesErrors.CustomFieldValueNotOfConfiguredType);
    }

    internal override Result CheckDependentFieldValue(object dependencyValue, object dependantValue, EqualityKind equalityKind)
    {
        var startDate = dependencyValue as DateOnly?;
        var endDate = dependantValue as DateOnly?;

        if (Equals(equalityKind, EqualityKind.Equal))
        {
            if (startDate.HasValue && startDate != endDate)
            {
                return Result.Failure(CustomEntitiesErrors.DatesShouldBeEquals);
            }

            return Result.Ok();
        }

        if (Equals(equalityKind, EqualityKind.NotEqual))
        {
            if (startDate.HasValue && startDate == endDate)
            {
                return Result.Failure(CustomEntitiesErrors.DatesShouldBeDifferent);
            }

            return Result.Ok();
        }

        if (endDate < startDate)
        {
            return Result.Failure(CustomEntitiesErrors.InvalidDateRange);
        }

        return Result.Ok();
    }
}