using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions;

namespace Raftel.Api.Server.Middlewares;

/// <summary>
/// Extension methods for registering the Wide Event middleware and services.
/// </summary>
public static class WideEventMiddlewareExtensions
{
    /// <summary>
    /// Registers the <see cref="IWideEvent"/> service as scoped (one instance per request).
    /// Must be called during service configuration before <see cref="UseRaftelWideEvent"/>.
    /// </summary>
    public static IServiceCollection AddRaftelWideEvent(this IServiceCollection services)
    {
        services.AddScoped<IWideEvent, WideEvent>();
        return services;
    }

    /// <summary>
    /// Adds the Wide Event middleware to the HTTP request pipeline.
    /// The middleware initializes a wide event, enriches it with HTTP context data,
    /// and emits a single structured log entry when the request completes.
    /// Requires <see cref="AddRaftelWideEvent"/> to be called during service configuration.
    /// </summary>
    public static IApplicationBuilder UseRaftelWideEvent(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<WideEventMiddleware>();
    }
}
