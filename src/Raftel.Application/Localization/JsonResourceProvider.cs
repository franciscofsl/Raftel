using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Raftel.Application.Localization;

/// <summary>
/// Provides localization resources from JSON files.
/// </summary>
public class JsonResourceProvider : IResourceProvider
{
    private readonly LocalizationOptions _options;
    private readonly IEnumerable<string> _searchPaths;

    public JsonResourceProvider(IOptions<LocalizationOptions> options, IEnumerable<string> searchPaths)
    {
        _options = options.Value;
        _searchPaths = searchPaths;
    }

    /// <inheritdoc />
    public async Task<LocalizationResource?> LoadResourceAsync(string moduleName, string culture)
    {
        foreach (var basePath in _searchPaths)
        {
            var filePath = Path.Combine(basePath, _options.ResourcesPath, moduleName, $"{culture}.json");

            if (!File.Exists(filePath))
            {
                continue;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var resource = JsonSerializer.Deserialize<LocalizationResource>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return resource;
            }
            catch (Exception ex)
            {
                // In production, use a proper logger to log the exception
                // For now, we'll silently skip the file and continue searching
                System.Diagnostics.Debug.WriteLine($"Failed to load localization file '{filePath}': {ex.Message}");
                continue;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAvailableModulesAsync()
    {
        var modules = new HashSet<string>();

        foreach (var basePath in _searchPaths)
        {
            var localizationPath = Path.Combine(basePath, _options.ResourcesPath);

            if (!Directory.Exists(localizationPath))
            {
                continue;
            }

            var directories = Directory.GetDirectories(localizationPath);
            foreach (var directory in directories)
            {
                modules.Add(Path.GetFileName(directory));
            }
        }

        return await Task.FromResult(modules);
    }
}
