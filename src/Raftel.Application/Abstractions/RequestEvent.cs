namespace Raftel.Application.Abstractions;

internal sealed class RequestEvent : IRequestEvent
{
    private static readonly string[] DeniedSubstrings = ["password", "token", "secret", "apikey", "authorization"];

    private readonly Dictionary<string, object> _fields = new();

    public IReadOnlyDictionary<string, object> Fields => _fields;

    public void Set(string field, object value)
    {
        if (IsDenied(field))
        {
            return;
        }

        _fields[field] = value;
    }

    public void Increment(string field, long amount = 1)
    {
        if (IsDenied(field))
        {
            return;
        }

        _fields[field] = _fields.TryGetValue(field, out var current) && current is long existing
            ? existing + amount
            : amount;
    }

    private static bool IsDenied(string field) =>
        DeniedSubstrings.Any(denied => field.Contains(denied, StringComparison.OrdinalIgnoreCase));
}
