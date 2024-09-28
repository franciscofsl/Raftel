using Raftel.Core.BaseTypes;
using Raftel.Core.CustomEntities.ValueObjects;
using Raftel.Shared.Results;

namespace Raftel.Core.CustomEntities.CustomFieldsTypes;

public abstract class CustomFieldBase : Entity<CustomFieldId>
{
    private readonly List<CustomFieldDependency> _fieldDependencies = new();

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

    public Result DependsOf(CustomFieldBase dependency, EqualityKind equalityKind)
    {
        if (_fieldDependencies.Any(_ => _.DependencyField.Id == dependency.Id))
        {
            return Result.Failure(CustomEntitiesErrors.FieldAlreadyHaveThisDependency);
        }

        _fieldDependencies.Add(new CustomFieldDependency(dependency, equalityKind));
        return Result.Ok();
    }

    internal Result CanBeUpdatedInEntity(CustomEntity customEntity, object value)
    {
        var validTypeResult = ValueHasValidType(value);

        if (validTypeResult.IsFailure)
        {
            return validTypeResult;
        }

        var validDependenciesResult = DependenciesHasValue(customEntity);
        if (validDependenciesResult.IsFailure)
        {
            return validDependenciesResult;
        }

        var validDependenciesValuesResult = FieldDependenciesHasValidValues(customEntity, value);
        if (validDependenciesValuesResult.IsFailure)
        {
            return validDependenciesValuesResult;
        }

        var isOptionalValueResult = IsOptionalValue(value);
        if (isOptionalValueResult.IsFailure)
        {
            return isOptionalValueResult;
        }

        var beforeUpdateValidationsResult = BeforeUpdateValidations(value);
        if (beforeUpdateValidationsResult.IsFailure)
        {
            return beforeUpdateValidationsResult;
        }

        return validTypeResult;
    }

    protected abstract Result ValueHasValidType(object value);

    protected virtual Result BeforeUpdateValidations(object value)
    {
        return Result.Ok();
    }

    private Result IsOptionalValue(object value)
    {
        if (IsRequired && value is null)
        {
            return Result.Failure(CustomEntitiesErrors.CustomFieldIsRequired);
        }

        return Result.Ok();
    }

    private Result DependenciesHasValue(CustomEntity customEntity)
    {
        foreach (var dependency in _fieldDependencies)
        {
            var value = customEntity.ValueOf(dependency.DependencyField);
            if (value is null)
            {
                return Result.Failure(CustomEntitiesErrors.DependencyNotHasValue);
            }
        }

        return Result.Ok();
    }

    private Result FieldDependenciesHasValidValues(CustomEntity customEntity, object newValue)
    {
        foreach (var fieldDependency in _fieldDependencies)
        {
            var result = fieldDependency.Validate(customEntity, newValue);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Ok();
    }

    internal virtual Result CheckDependentFieldValue(object dependencyValue, object dependantValue,
        EqualityKind equalityKind)
    {
        return Result.Ok();
    }
}