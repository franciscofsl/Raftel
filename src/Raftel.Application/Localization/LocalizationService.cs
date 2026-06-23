using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Raftel.Application.Localization;

/// <summary>
/// Service for managing localization and translations.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly IResourceProvider _resourceProvider;
    private readonly LocalizationOptions _options;
    private readonly IMemoryCache _cache;

    public LocalizationService(
        IResourceProvider resourceProvider,
        IOptions<LocalizationOptions> options,
        IMemoryCache cache)
    {
        _resourceProvider = resourceProvider;
        _options = options.Value;
        _cache = cache;
    }

    /// <inheritdoc />
    public string GetString(string key, string culture, string? moduleName = null)
    {
        return GetString(key, culture, Array.Empty<object>(), moduleName);
    }

    /// <inheritdoc />
    public string GetString(string key, string culture, object[] arguments, string? moduleName = null)
    {
        var text = GetLocalizedText(key, culture, moduleName);

        if (arguments.Length > 0)
        {
            try
            {
                return string.Format(text, arguments);
            }
            catch (FormatException)
            {
                // Return unformatted text if format arguments don't match
                return text;
            }
        }

        return text;
    }

    /// <inheritdoc />
    public async Task<LocalizationResource> GetResourcesAsync(string culture, IEnumerable<string>? moduleNames = null)
    {
        var result = new LocalizationResource
        {
            Culture = culture,
            Texts = new Dictionary<string, string>()
        };

        var modules = moduleNames?.ToList() ?? (await _resourceProvider.GetAvailableModulesAsync()).ToList();

        foreach (var moduleName in modules)
        {
            var resource = await LoadResourceWithCacheAsync(moduleName, culture);

            if (resource != null)
            {
                foreach (var kvp in resource.Texts)
                {
                    // Avoid overwriting keys - first module wins
                    if (!result.Texts.ContainsKey(kvp.Key))
                    {
                        result.Texts[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        return result;
    }

    private string GetLocalizedText(string key, string culture, string? moduleName)
    {
        if (!string.IsNullOrEmpty(moduleName))
        {
            var text = GetTextFromModule(key, culture, moduleName);
            if (text != null)
            {
                return text;
            }
        }

        // Try to find in all modules - use ConfigureAwait(false) to avoid deadlock
        var allModules = _resourceProvider.GetAvailableModulesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        foreach (var module in allModules)
        {
            var text = GetTextFromModule(key, culture, module);
            if (text != null)
            {
                return text;
            }
        }

        // Fallback to default culture if enabled
        if (_options.UseFallbackCulture && culture != _options.DefaultCulture)
        {
            return GetLocalizedText(key, _options.DefaultCulture, moduleName);
        }

        return key;
    }

    private string? GetTextFromModule(string key, string culture, string moduleName)
    {
        // Use ConfigureAwait(false) to avoid potential deadlocks
        var resource = LoadResourceWithCacheAsync(moduleName, culture).ConfigureAwait(false).GetAwaiter().GetResult();

        if (resource?.Texts.TryGetValue(key, out var text) == true)
        {
            return text;
        }

        return null;
    }

    private async Task<LocalizationResource?> LoadResourceWithCacheAsync(string moduleName, string culture)
    {
        if (!_options.EnableCaching)
        {
            return await _resourceProvider.LoadResourceAsync(moduleName, culture);
        }

        var cacheKey = $"Localization_{moduleName}_{culture}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            return await _resourceProvider.LoadResourceAsync(moduleName, culture);
        });
    }
}
