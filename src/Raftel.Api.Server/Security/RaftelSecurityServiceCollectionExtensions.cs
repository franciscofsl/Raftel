using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace Raftel.Api.Server.Security;

public static class RaftelSecurityServiceCollectionExtensions
{
    /// <summary>
    /// Registers rate limiting, CORS, security headers and forwarded headers. Pair it with
    /// <see cref="RaftelSecurityApplicationBuilderExtensions.UseRaftelSecurity"/> and
    /// <see cref="RaftelSecurityApplicationBuilderExtensions.UseRaftelRateLimiting"/>.
    /// </summary>
    /// <remarks>
    /// Settings come from the <see cref="RaftelSecurityOptions.SectionName"/> section, then from
    /// <paramref name="configure"/>, and are validated when the host starts.
    /// </remarks>
    public static IServiceCollection AddRaftelSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RaftelSecurityOptions> configure = null)
    {
        services.AddOptions<RaftelSecurityOptions>()
            .Bind(configuration.GetSection(RaftelSecurityOptions.SectionName))
            .Configure(options => configure?.Invoke(options))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<RaftelSecurityOptions>, RaftelSecurityOptionsValidator>());

        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RaftelSecurityOptions>>((rateLimiter, security) =>
                ConfigureRateLimiter(rateLimiter, security.Value.RateLimiting));

        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<RaftelSecurityOptions>, IHostEnvironment, ILoggerFactory>((cors, security, environment, loggers) =>
                ConfigureCors(cors, security.Value.Cors, environment, loggers.CreateLogger(typeof(RaftelSecurityOptions))));

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<RaftelSecurityOptions>>((forwarded, security) =>
                ConfigureForwardedHeaders(forwarded, security.Value.ForwardedHeaders));

        services.Configure<KestrelServerOptions>(kestrel => kestrel.AddServerHeader = false);

        return services;
    }

    private static void ConfigureRateLimiter(RateLimiterOptions rateLimiter, RaftelRateLimitOptions options)
    {
        rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        rateLimiter.OnRejected = RateLimitRejection.OnRejected;
        rateLimiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            context => RateLimitPartitions.Global(context, options.Global));
        rateLimiter.AddPolicy(RaftelRateLimitPolicies.Auth, context => RateLimitPartitions.Auth(context, options.Auth));
        rateLimiter.AddPolicy(RaftelRateLimitPolicies.Write, context => RateLimitPartitions.Write(context, options.Write));
    }

    private static void ConfigureCors(CorsOptions cors, RaftelCorsOptions options, IHostEnvironment environment, ILogger logger)
    {
        var policy = new CorsPolicyBuilder()
            .WithMethods(options.EffectiveMethods)
            .WithHeaders(options.EffectiveHeaders)
            .WithExposedHeaders(options.EffectiveExposedHeaders)
            .SetPreflightMaxAge(options.PreflightMaxAge);

        if (options.AllowDevelopmentOrigins && environment.IsDevelopment())
        {
            logger.LogWarning("CORS allows any origin because AllowDevelopmentOrigins is enabled in Development.");
            policy.SetIsOriginAllowed(_ => true);
        }
        else
        {
            if (options.AllowDevelopmentOrigins)
            {
                logger.LogWarning(
                    "CORS AllowDevelopmentOrigins is ignored in the {Environment} environment.",
                    environment.EnvironmentName);
            }

            if (options.AllowedOrigins.Length == 0)
            {
                return;
            }

            if (options.AllowsAnyOrigin)
            {
                policy.AllowAnyOrigin();
            }
            else
            {
                policy.WithOrigins(options.AllowedOrigins);
            }
        }

        if (options.AllowCredentials)
        {
            policy.AllowCredentials();
        }

        cors.AddDefaultPolicy(policy.Build());
    }

    private static void ConfigureForwardedHeaders(ForwardedHeadersOptions forwarded, RaftelForwardedHeadersOptions options)
    {
        forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        forwarded.ForwardLimit = options.ForwardLimit;

        foreach (var proxy in options.KnownProxies)
        {
            forwarded.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        foreach (var network in options.KnownNetworks)
        {
            forwarded.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }
    }
}
