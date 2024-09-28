using Raftel.Core.GuardClauses;
using Raftel.Shared.GuidGenerators;

namespace Raftel.Core.Auditing;

public class PropertyChange
{
    [ExcludeFromCodeCoverage]
    private PropertyChange()
    {
        /* For ORM */
    }

    public PropertyChange(string name, string typeName, string oldValue, string newValue)
    {
        Id = SequentialGuidGenerator.Create();
        Name = Ensure.NotNullOrEmpty(name, nameof(name));
        TypeName = Ensure.NotNullOrEmpty(typeName, nameof(typeName));
        OldValue = oldValue;
        NewValue = newValue;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string TypeName { get; private set; }
    public string OldValue { get; private set; }
    public string NewValue { get; private set; }
}