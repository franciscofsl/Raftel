﻿using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Tests.Abstractions;

public sealed record TestQuery() : IQuery<string>;

public sealed class TestQueryHandler(ISpy spy) : IQueryHandler<TestQuery, string>
{
    public Task<Result<string>> HandleAsync(TestQuery request)
    {
        spy.Intercept("Query");
        return Task.FromResult<Result<string>>("Query");
    }
}