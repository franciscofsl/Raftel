namespace Raftel.Application.Abstractions.Outbox;

internal sealed class DomainEventTypeRegistry : IDomainEventTypeRegistry
{
    private readonly Dictionary<Type, string> _namesByType = [];
    private readonly Dictionary<string, Type> _typesByName = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, List<Type>> _handlersByType = [];

    public void Register(Type eventType, Type handlerType)
    {
        if (!_handlersByType.TryGetValue(eventType, out var handlers))
        {
            handlers = [];
            _handlersByType[eventType] = handlers;

            var name = NameOf(eventType);
            _namesByType[eventType] = name;
            _typesByName[name] = eventType;
        }

        if (!handlers.Contains(handlerType))
        {
            handlers.Add(handlerType);
        }
    }

    public bool TryGetName(Type eventType, out string name) => _namesByType.TryGetValue(eventType, out name);

    public bool TryGetEventType(string name, out Type eventType)
    {
        eventType = null;
        return name is not null && _typesByName.TryGetValue(name, out eventType);
    }

    public IReadOnlyList<Type> GetHandlerTypes(Type eventType) =>
        _handlersByType.TryGetValue(eventType, out var handlers) ? handlers : [];

    private static string NameOf(Type eventType) => $"{eventType.FullName}, {eventType.Assembly.GetName().Name}";
}
