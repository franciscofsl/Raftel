using Raftel.Core.BaseTypes;
using Raftel.Core.CustomEntities.CustomFieldsTypes;
using Raftel.Core.CustomEntities.ValueObjects;

namespace Raftel.Core.CustomEntities;

public class CustomEntityConfiguration : AggregateRoot<CustomEntityConfigurationId>
{
    private readonly List<CustomFieldBase> _fields = [];

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

    public IReadOnlyCollection<CustomFieldBase> Fields { get; private set; }

    public CustomEntity NewEntity()
    {
        return CustomEntity.Create(this);
    }

    public CustomFieldBase AddCustomField(string key, string name, CustomFieldKind kind, bool isRequired = false)
    { 
        var customField = CustomFieldBase.Create(key, name, kind, isRequired);
        _fields.Add(customField);
        return customField;
    }

    internal bool ContainsField(CustomFieldBase customFieldBase)
    {
        return _fields.Contains(customFieldBase);
    }
}