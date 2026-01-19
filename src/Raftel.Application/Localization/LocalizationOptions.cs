namespace Raftel.Application.Localization;

/// <summary>
/// Configuration options for the localization system.
/// </summary>
public class LocalizationOptions
{
    /// <summary>
    /// Gets or sets the default culture to use when no culture is specified.
    /// </summary>
    public string DefaultCulture { get; set; } = "en";

    /// <summary>
    /// Gets or sets the list of supported cultures.
    /// </summary>
    public List<string> SupportedCultures { get; set; } = new() { "en", "es" };

    /// <summary>
    /// Gets or sets the base path for localization resources.
    /// </summary>
    public string ResourcesPath { get; set; } = "Localization";

    /// <summary>
    /// Gets or sets whether to enable caching of localization resources.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use fallback to default culture when a key is not found.
    /// </summary>
    public bool UseFallbackCulture { get; set; } = true;
}
