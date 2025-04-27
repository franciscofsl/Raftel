using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Tests.Abstractions;

public class TestCommandHandler(ISpy spy) : ICommandHandler<TestCommand>
{
    public Task<Result> HandleAsync(TestCommand request, CancellationToken token = default)
    {
        spy.Intercept("Handler");
        return Task.FromResult(Result.Success());
    }
}