using Raftel.Application.Queries;

namespace Raftel.Application.UnitTests.Abstractions;

public sealed record TestQuery() : IQuery<string>;