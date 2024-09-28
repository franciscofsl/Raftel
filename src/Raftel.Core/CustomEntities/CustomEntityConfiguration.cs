using Raftel.Core.BaseTypes;
using Raftel.Core.CustomEntities.ValueObjects;

namespace Raftel.Core.CustomEntities;

public class CustomEntityConfiguration : AggregateRoot<CustomEntityConfigurationId>
{
    [ExcludeFromCodeCoverage]
    private CustomEntityConfiguration()
    {
        /* For ORM */
    }

    public static CustomEntityConfiguration Create(string key, string singularName, string pluralName)
    {
        return new CustomEntityConfiguration
        {
            Id = EntityIdGenerator.Create<CustomEntityConfigurationId>(),
            Key = key.Replace(" ", string.Empty).Trim(),
            SingularName = singularName,
            PluralName = pluralName
        };
    }

    public string Key { get; private init; }
    public string SingularName { get; private set; }
    public string PluralName { get; private set; }
    public bool Visible { get; set; }

    public CustomFieldsConfiguration CustomFields { get; private set; }

    public CustomEntity NewEntity()
    {
        return CustomEntity.Create(this);
    }
}

public class CustomFieldsConfiguration
{
}