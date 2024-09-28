using Raftel.Core.CustomEntities;

namespace Raftel.Core.Tests.CustomEntities;

public class CustomEntityConfigurationBuilder
{
    private string _key = "defaultKey";
    private string _singularName = "defaultSingular";
    private string _pluralName = "defaultPlural";
    private bool _visible = true;
    private CustomFieldsConfiguration _customFields = new CustomFieldsConfiguration();

    public static CustomEntityConfigurationBuilder Instance()
    {
        return new CustomEntityConfigurationBuilder();
    }

    public CustomEntityConfigurationBuilder WithKey(string key)
    {
        _key = key;
        return this;
    }

    public CustomEntityConfigurationBuilder WithSingularName(string singularName)
    {
        _singularName = singularName;
        return this;
    }

    public CustomEntityConfigurationBuilder WithPluralName(string pluralName)
    {
        _pluralName = pluralName;
        return this;
    }

    public CustomEntityConfigurationBuilder WithVisibility(bool visible)
    {
        _visible = visible;
        return this;
    }

    public CustomEntityConfigurationBuilder WithCustomFields(CustomFieldsConfiguration customFields)
    {
        _customFields = customFields;
        return this;
    }

    public CustomEntityConfiguration Build()
    {
        var entityConfiguration = CustomEntityConfiguration.Create(_key, _singularName, _pluralName);
        entityConfiguration.Visible = _visible;
        var customFieldsField = typeof(CustomEntityConfiguration)
            .GetField("<CustomFields>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        customFieldsField?.SetValue(entityConfiguration, _customFields);

        return entityConfiguration;
    }
}