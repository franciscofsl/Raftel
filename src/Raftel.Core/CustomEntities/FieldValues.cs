using Raftel.Core.CustomEntities.ValueObjects;

namespace Raftel.Core.CustomEntities;

public class FieldValues
{
    private readonly Dictionary<CustomFieldId, object> _values = new();

    public object this[CustomFieldId customFieldId]
    {
        get => _values.TryGetValue(customFieldId, out var value) ? value : null;
        set => _values[customFieldId] = value;
    }
}