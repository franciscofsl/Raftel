using Raftel.Application.Queries;

namespace Raftel.Application.Tests.Abstractions;

public sealed record TestQuery() : IQuery<string>;