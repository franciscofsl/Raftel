using Raftel.Core.BaseTypes;
using Raftel.Core.CustomEntities.ValueObjects;
using Raftel.Shared.GuidGenerators;

namespace Raftel.Core.CustomEntities;

public class CustomEntity : AggregateRoot<CustomEntityId>
{
    private readonly CustomEntityConfiguration _configuration;

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
}