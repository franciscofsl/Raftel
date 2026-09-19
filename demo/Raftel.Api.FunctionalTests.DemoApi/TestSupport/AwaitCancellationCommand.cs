using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Api.FunctionalTests.DemoApi.TestSupport;

public sealed record AwaitCancellationCommand : ICommand;

public static class AwaitCancellationTracker
{
    private static int _completions;

    public static int Completions => _completions;

    public static void Increment() => Interlocked.Increment(ref _completions);
}

internal sealed class AwaitCancellationCommandHandler : ICommandHandler<AwaitCancellationCommand>
{
    public async Task<Result> HandleAsync(AwaitCancellationCommand request, CancellationToken token = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), token);
        AwaitCancellationTracker.Increment();
        return Result.Success();
    }
}
