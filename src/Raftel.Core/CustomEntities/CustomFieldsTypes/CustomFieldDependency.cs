using Raftel.Shared.Results;

namespace Raftel.Core.CustomEntities.CustomFieldsTypes;

public record CustomFieldDependency(CustomFieldBase DependencyField, EqualityKind EqualityKind)
{
    public Result Validate(CustomEntity customEntity, object dependantValue)
    {
        var dependencyValue = customEntity.ValueOf(DependencyField);
        return DependencyField.CheckDependentFieldValue(dependencyValue, dependantValue, EqualityKind);
    }
};