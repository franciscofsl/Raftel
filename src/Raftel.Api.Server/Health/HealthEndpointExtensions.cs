using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Raftel.Application.Abstractions.Health;

namespace Raftel.Api.Server.Health;

public static class HealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapRaftelHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<HealthOptions>>().Value;

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthResponseWriter.WriteStatusOnly
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready),
            ResponseWriter = HealthResponseWriter.WriteStatusOnly
        }).AllowAnonymous();

        var detailedEndpoint = endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = HealthResponseWriter.WriteDetailed
        });

        if (options.RequireAuthorizationForDetails)
        {
            detailedEndpoint.RequireAuthorization(BuildAuthorizeAttribute(options.DetailsPolicy));
        }
        else
        {
            detailedEndpoint.AllowAnonymous();
        }

        return endpoints;
    }

    private static Microsoft.AspNetCore.Authorization.AuthorizeAttribute BuildAuthorizeAttribute(string policy)
    {
        var attribute = new Microsoft.AspNetCore.Authorization.AuthorizeAttribute();
        if (!string.IsNullOrWhiteSpace(policy))
        {
            attribute.Policy = policy;
        }

        return attribute;
    }
}
