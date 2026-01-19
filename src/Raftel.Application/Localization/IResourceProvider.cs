namespace Raftel.Application.Localization;

/// <summary>
/// Defines a contract for providing localization resources from different sources.
/// </summary>
public interface IResourceProvider
{
    /// <summary>
    /// Loads localization resources for the specified module and culture.
    /// </summary>
    /// <param name="moduleName">The name of the module.</param>
    /// <param name="culture">The culture code.</param>
    /// <returns>A LocalizationResource containing the translations, or null if not found.</returns>
    Task<LocalizationResource?> LoadResourceAsync(string moduleName, string culture);

    /// <summary>
    /// Gets all available modules that have localization resources.
    /// </summary>
    /// <returns>A list of module names.</returns>
    Task<IEnumerable<string>> GetAvailableModulesAsync();
}
