using Raftel.Domain.Auditing.ValueObjects;
using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Auditing;

/// <summary>
/// Represents a single property value change captured for an audited entity.
/// </summary>
public sealed class PropertyChange : Entity<PropertyChangeId>
{
    private PropertyChange(
        PropertyChangeId id,
        string propertyName,
        string propertyType,
        string? oldValue,
        string? newValue) : base(id)
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
        OldValue = oldValue;
        NewValue = newValue;
    }

    private PropertyChange()
    {
    }

    public string PropertyName { get; private set; }
    public string PropertyType { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }

    public static PropertyChange Create(string propertyName, string propertyType, string? oldValue, string? newValue) =>
        new(PropertyChangeId.New(), propertyName, propertyType, oldValue, newValue);
}
