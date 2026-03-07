using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.UnitTests.Abstractions;

public sealed class TestCommandWithResultHandler(ISpy spy) : ICommandHandler<TestCommandWithResult, string>
{
    public Task<Result<string>> HandleAsync(TestCommandWithResult request, CancellationToken token = default)
    {
        spy.Intercept("HandlerWithResult");
        return Task.FromResult<Result<string>>(request.Message);
    }
}
