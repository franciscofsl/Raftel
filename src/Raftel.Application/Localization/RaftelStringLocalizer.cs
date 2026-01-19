using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Raftel.Application.Localization;

/// <summary>
/// Custom implementation of IStringLocalizer that uses the LocalizationService.
/// </summary>
public class RaftelStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly ILocalizationService _localizationService;
    private readonly string _moduleName;

    public RaftelStringLocalizer(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        _moduleName = typeof(T).Name;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            var value = _localizationService.GetString(name, culture, _moduleName);
            return new LocalizedString(name, value, resourceNotFound: value == name);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            var value = _localizationService.GetString(name, culture, arguments, _moduleName);
            return new LocalizedString(name, value, resourceNotFound: value == name);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var resource = _localizationService.GetResourcesAsync(culture, new[] { _moduleName }).GetAwaiter().GetResult();

        return resource.Texts.Select(kvp => new LocalizedString(kvp.Key, kvp.Value, resourceNotFound: false));
    }
}
