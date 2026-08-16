using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Api.FunctionalTests.DemoApi.TestSupport;

public sealed record CreateTestResourceCommand(string Name) : ICommand<Guid>;

internal sealed class CreateTestResourceCommandHandler : ICommandHandler<CreateTestResourceCommand, Guid>
{
    public Task<Result<Guid>> HandleAsync(CreateTestResourceCommand request, CancellationToken token = default) =>
        Task.FromResult(Result.Success(Guid.NewGuid()));
}
