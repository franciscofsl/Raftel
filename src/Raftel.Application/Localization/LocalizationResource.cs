namespace Raftel.Application.Localization;

/// <summary>
/// Represents a localization resource containing translations for a specific culture.
/// </summary>
public class LocalizationResource
{
    /// <summary>
    /// Gets or sets the culture code (e.g., "en", "es", "fr").
    /// </summary>
    public string Culture { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dictionary of translation texts.
    /// Key is the resource key, Value is the translated text.
    /// </summary>
    public Dictionary<string, string> Texts { get; set; } = new();
}
