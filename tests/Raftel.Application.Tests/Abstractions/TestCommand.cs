using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Tests.Abstractions;

public sealed record TestCommand() : ICommand;

public class TestCommandHandler(ISpy spy) : ICommandHandler<TestCommand>
{
    public Task<Result> HandleAsync(TestCommand request)
    {
        spy.Intercept("Handler");
        return Task.FromResult(Result.Success());
    }
}