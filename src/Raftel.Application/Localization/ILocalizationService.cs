namespace Raftel.Application.Localization;

/// <summary>
/// Defines a contract for the localization service.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets a localized string for the specified key and culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The culture code.</param>
    /// <param name="moduleName">The module name (optional).</param>
    /// <returns>The localized string, or the key itself if not found.</returns>
    string GetString(string key, string culture, string? moduleName = null);

    /// <summary>
    /// Gets a localized string with format arguments.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The culture code.</param>
    /// <param name="arguments">The format arguments.</param>
    /// <param name="moduleName">The module name (optional).</param>
    /// <returns>The formatted localized string.</returns>
    string GetString(string key, string culture, object[] arguments, string? moduleName = null);

    /// <summary>
    /// Gets all localized strings for the specified culture and optional modules.
    /// </summary>
    /// <param name="culture">The culture code.</param>
    /// <param name="moduleNames">The module names to include (optional, null for all modules).</param>
    /// <returns>A LocalizationResource containing all translations.</returns>
    Task<LocalizationResource> GetResourcesAsync(string culture, IEnumerable<string>? moduleNames = null);
}
