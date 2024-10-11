using Raftel.Core.CustomEntities;

namespace Raftel.Core.Tests.CustomEntities;

public class CustomEntityConfigurationBuilder
{
    private string _key = "defaultKey";
    private string _singularName = "defaultSingular";
    private string _pluralName = "defaultPlural";
    private bool _visible = true; 

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
 

    public CustomEntityConfiguration Build()
    {
        var entityConfiguration = CustomEntityConfiguration.Create(_key, _singularName, _pluralName);
        entityConfiguration.Visible = _visible;
     

        return entityConfiguration;
    }
}