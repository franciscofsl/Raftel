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

    public void AddCommand<TRequest>(string route, HttpMethod method)
        where TRequest : ICommand
    {
        Commands.Add(new CommandDefinition(typeof(TRequest), route, method));
    }
}