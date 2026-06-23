using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Raftel.Application.Localization;

/// <summary>
/// Extension methods for configuring localization in the Raftel application.
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Adds localization services to the application.
    /// </summary>
    /// <param name="builder">The Raftel application builder.</param>
    /// <param name="configure">Optional configuration action for localization options.</param>
    /// <param name="searchPaths">Paths to search for localization resources.</param>
    /// <returns>The builder for chaining.</returns>
    public static IRaftelApplicationBuilder AddLocalization(
        this IRaftelApplicationBuilder builder,
        Action<LocalizationOptions>? configure = null,
        params string[] searchPaths)
    {
        return builder;
    }

    /// <summary>
    /// Adds localization services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for localization options.</param>
    /// <param name="searchPaths">Paths to search for localization resources.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRaftelLocalization(
        this IServiceCollection services,
        Action<LocalizationOptions>? configure = null,
        params string[] searchPaths)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<LocalizationOptions>(options => { });
        }

        // Register search paths as singleton
        services.AddSingleton<IEnumerable<string>>(searchPaths.Length > 0
            ? searchPaths
            : new[] { AppDomain.CurrentDomain.BaseDirectory });

        // Register localization services
        services.AddMemoryCache();
        services.AddSingleton<IResourceProvider, JsonResourceProvider>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IStringLocalizerFactory, RaftelStringLocalizerFactory>();
        services.AddTransient(typeof(IStringLocalizer<>), typeof(RaftelStringLocalizer<>));

        return services;
    }
}
