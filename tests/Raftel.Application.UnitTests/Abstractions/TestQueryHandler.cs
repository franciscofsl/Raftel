using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.UnitTests.Abstractions;

public sealed class TestQueryHandler(ISpy spy) : IQueryHandler<TestQuery, string>
{
    public Task<Result<string>> HandleAsync(TestQuery request, CancellationToken token = default)
    {
        spy.Intercept("Query");
        return Task.FromResult<Result<string>>("Query");
    }
}