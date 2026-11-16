using Raftel.Application.Commands;
using Raftel.Application.Queries;

namespace Raftel.Api.Server.AutoEndpoints;

public sealed class RouteOptions
{
    public string Name { get; set; }
    public string BaseUri { get; set; }

    internal List<QueryDefinition> Queries { get; } = new();
    internal List<CommandDefinition> Commands { get; } = new();

    public RouteOptions AddQuery<TRequest, TResult>(string route, HttpMethod method)
        where TRequest : IQuery<TResult>
    {
        Queries.Add(new QueryDefinition(typeof(TRequest), typeof(TResult), route, method));
        return this;
    }

    /// <param name="route">Route relative to the group's base URI.</param>
    /// <param name="method">HTTP method: POST, PUT or DELETE.</param>
    /// <param name="rateLimitPolicy">
    /// Rate limiting policy for the endpoint. Defaults to <see cref="Security.RaftelRateLimitPolicies.Write"/>.
    /// </param>
    public RouteOptions AddCommand<TRequest>(string route, HttpMethod method, string? rateLimitPolicy = null)
        where TRequest : ICommand
    {
        var definition = new CommandDefinition(typeof(TRequest), null, route, method,
            RateLimitPolicy: rateLimitPolicy);
        Commands.Add(definition);
        return this;
    }

    /// <param name="route">Route relative to the group's base URI.</param>
    /// <param name="method">HTTP method: POST, PUT or DELETE.</param>
    /// <param name="createdRouteName">Route name used to build the <c>201 Created</c> location, if any.</param>
    /// <param name="rateLimitPolicy">
    /// Rate limiting policy for the endpoint. Defaults to <see cref="Security.RaftelRateLimitPolicies.Write"/>.
    /// </param>
    public RouteOptions AddCommand<TRequest, TResult>(string route, HttpMethod method, string? createdRouteName = null,
        string? rateLimitPolicy = null)
        where TRequest : ICommand<TResult>
    {
        var definition = new CommandDefinition(typeof(TRequest), typeof(TResult), route, method, createdRouteName,
            rateLimitPolicy);
        Commands.Add(definition);
        return this;
    }
}