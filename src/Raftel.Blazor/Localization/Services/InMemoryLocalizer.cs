using Microsoft.Extensions.Localization;

namespace Raftel.Blazor.Localization.Services;

public class InMemoryLocalizer(IDictionary<string, string> resources) : IStringLocalizer
{
    public LocalizedString this[string name] =>
        resources.TryGetValue(name, out var value)
            ? new LocalizedString(name, value, true)
            : new LocalizedString(name, name, false);

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (!resources.TryGetValue(name, out var value))
            {
                return new LocalizedString(name, name, false);
            }

            var formattedValue = string.Format(value, arguments);
            return new LocalizedString(name, formattedValue, true);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        foreach (var translation in resources)
        {
            yield return new LocalizedString(translation.Key, translation.Value, true);
        }
    }
}