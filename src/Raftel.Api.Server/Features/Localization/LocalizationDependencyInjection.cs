using Microsoft.AspNetCore.Builder;
using Raftel.Application.Localization;

namespace Raftel.Api.Server.Features.Localization;

/// <summary>
/// Extension methods for configuring localization endpoints.
/// </summary>
public static class LocalizationDependencyInjection
{
    /// <summary>
    /// Adds Raftel localization endpoints and middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder AddRaftelLocalization(this IApplicationBuilder app)
    {
        app.UseMiddleware<LocalizationMiddleware>();
        return app;
    }
}
