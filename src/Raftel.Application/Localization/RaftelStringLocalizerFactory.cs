using Microsoft.Extensions.Localization;

namespace Raftel.Application.Localization;

/// <summary>
/// Factory for creating RaftelStringLocalizer instances.
/// </summary>
public class RaftelStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly ILocalizationService _localizationService;

    public RaftelStringLocalizerFactory(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        var localizerType = typeof(RaftelStringLocalizer<>).MakeGenericType(resourceSource);
        return (IStringLocalizer)Activator.CreateInstance(localizerType, _localizationService)!;
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        // For non-generic IStringLocalizer, we use a default implementation
        return new RaftelStringLocalizer<object>(_localizationService);
    }
}
