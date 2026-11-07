using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.UnitTests.Abstractions;

public sealed class ProtectedTestCommandHandler(ISpy spy) : ICommandHandler<ProtectedTestCommand>
{
    public Task<Result> HandleAsync(ProtectedTestCommand request, CancellationToken token = default)
    {
        spy.Intercept("Protected handler");
        return Task.FromResult(Result.Success());
    }
}
