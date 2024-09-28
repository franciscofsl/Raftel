using Raftel.Core.BaseTypes;
using Raftel.Core.CustomEntities.CustomFieldsTypes;
using Raftel.Core.CustomEntities.ValueObjects;
using Raftel.Shared.Results;

namespace Raftel.Core.CustomEntities;

public class CustomEntity : AggregateRoot<CustomEntityId>
{
    private readonly CustomEntityConfiguration _configuration;
    private readonly FieldValues _values = new();

    [ExcludeFromCodeCoverage]
    private CustomEntity()
    {
        /* For ORM */
    }

    [ExcludeFromCodeCoverage]
    private CustomEntity(CustomEntityConfiguration configuration)
    {
        _configuration = configuration;
    }

    internal static CustomEntity Create(CustomEntityConfiguration configuration)
    {
        return new CustomEntity(configuration)
        {
            Id = EntityIdGenerator.Create<CustomEntityId>(),
        };
    }

    public Result UpdateField(CustomFieldBase customFieldBase, object value)
    {
        if (!_configuration.ContainsField(customFieldBase))
        {
            return Result.Failure(CustomEntitiesErrors.CustomFieldNotSupportedByEntity);
        }

        var result = customFieldBase.CanBeUpdatedInEntity(this, value);
        if (result.IsFailure)
        {
            return result;
        }
        
        _values[customFieldBase.Id] = value;
        return Result.Ok();
    }

    public object ValueOf(CustomFieldBase customFieldBase)
    {
        return _values[customFieldBase.Id];
    }
}