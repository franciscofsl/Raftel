using Raftel.Core.BaseTypes;
using Raftel.Core.CustomEntities.ValueObjects;
using Raftel.Shared.Results;

namespace Raftel.Core.CustomEntities.CustomFieldsTypes;

public abstract class CustomFieldBase : Entity<CustomFieldId>
{
    [ExcludeFromCodeCoverage]
    protected CustomFieldBase()
    {
        /* For ORM */
    }

    protected CustomFieldBase(string key, string name, bool isRequired)
    {
        Id = EntityIdGenerator.Create<CustomFieldId>();
        Key = key;
        Name = name;
        IsRequired = isRequired;
    }

    internal static CustomFieldBase Create(string key, string name, CustomFieldKind kind, bool isRequired)
    {
        return kind.Id switch
        {
            (int)CustomFieldKind.CustomFieldKindId.Text => new TextCustomField(key, name, isRequired),
            (int)CustomFieldKind.CustomFieldKindId.Int => new IntCustomField(key, name, isRequired),
            (int)CustomFieldKind.CustomFieldKindId.Decimal => new DecimalCustomField(key, name, isRequired),
            (int)CustomFieldKind.CustomFieldKindId.Boolean => new BooleanCustomField(key, name, isRequired),
            (int)CustomFieldKind.CustomFieldKindId.Date => new DateCustomField(key, name, isRequired),
            (int)CustomFieldKind.CustomFieldKindId.Time => new TimeCustomField(key, name, isRequired),
            (int)CustomFieldKind.CustomFieldKindId.DateTime => new DateTimeCustomField(key, name, isRequired),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), $"Unsupported custom field kind: {kind.Id}")
        };
    }
    public abstract CustomFieldKind Kind { get; }

    public bool IsRequired { get; private set; }

    public string Key { get; private init; }

    public string Name { get; private init; }

    internal Result CanBeUpdatedInEntity(object value)
    {
        var validTypeResult = ValueHasValidType(value);
        return validTypeResult.Success
            ? IsOptionalValue(value)
            : validTypeResult;
    }

    protected abstract Result ValueHasValidType(object value);

    protected Result IsOptionalValue(object value)
    {
        if (IsRequired && value is null)
        {
            return Result.Failure(CustomEntitiesErrors.CustomFieldIsRequired);
        }

        return Result.Ok();
    }
}