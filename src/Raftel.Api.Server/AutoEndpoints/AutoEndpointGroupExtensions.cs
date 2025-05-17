using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Raftel.Api.Server.AutoEndpoints;

public static class AutoEndpointGroupExtensions
{
    public static IEndpointRouteBuilder AddEndpointGroup(this IEndpointRouteBuilder app, Action<RouteOptions> configure)
    {
        var options = new RouteOptions();
        configure(options);
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var baseUri = options.BaseUri ?? $"/api/{options.Name?.ToLowerInvariant() ?? "default"}";

        var group = app.MapGroup(baseUri)
            .WithTags(options.Name)
            .WithOpenApi();

        foreach (var query in options.Queries)
        {
            var method = typeof(QueryEndpointMapper)
                .GetMethod(nameof(QueryEndpointMapper.MapQueryEndpoint))!
                .MakeGenericMethod(query.Request, query.Result);

            method.Invoke(null, new object[] { group, query.Route, query.Method });
        }

        foreach (var command in options.Commands)
        {
            var method = typeof(CommandEndpointMapper)
                .GetMethod(nameof(CommandEndpointMapper.MapCommandEndpoint))!
                .MakeGenericMethod(command.Request);

            method.Invoke(null, new object[] { group, command.Route, command.Method });
        }

        return app;
    }
}