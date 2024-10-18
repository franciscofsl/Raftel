using Microsoft.Extensions.Localization;

namespace Raftel.Blazor.Localization.Services;

public class InMemoryStringLocalizerFactory(IServiceProvider serviceProvider) : IStringLocalizerFactory
{
    public IStringLocalizer Create(Type resourceSource)
    {
        return serviceProvider.GetService(typeof(IStringLocalizer)) as IStringLocalizer;
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return serviceProvider.GetService(typeof(IStringLocalizer)) as IStringLocalizer;
    }
}