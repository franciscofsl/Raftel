using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Api.Server.AutoEndpoints;

public sealed class RouteOptions
{
    public string Name { get; set; }
    public string BaseUri { get; set; }

    internal List<QueryDefinition> Queries { get; } = new();

    public RouteOptions AddQuery<TRequest, TResult>(string route, HttpMethod method)
        where TRequest : IRequest<Result<TResult>>
    {
        Queries.Add(new QueryDefinition(typeof(TRequest), typeof(TResult), route, method));
        return this;
    }
}