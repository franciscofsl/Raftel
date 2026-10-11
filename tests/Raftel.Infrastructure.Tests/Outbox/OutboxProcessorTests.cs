using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Raftel.Infrastructure.Outbox;

namespace Raftel.Infrastructure.Tests.Outbox;

public class OutboxProcessorTests
{
    private static OutboxOptions Options(TimeSpan pollingInterval) =>
        new() { Enabled = true, BatchSize = 5, PollingInterval = pollingInterval };

    private static OutboxProcessor NewProcessor(IOutboxBatchRunner runner, OutboxOptions options) =>
        new(runner, options, TimeProvider.System, NullLogger<OutboxProcessor>.Instance);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10);
        }
    }

    [Fact]
    public async Task Processor_ShouldStopPromptly_WhenTheHostShutsDown()
    {
        var runner = new ScriptedRunner(_ => 0);
        var processor = NewProcessor(runner, Options(TimeSpan.FromHours(1)));

        await processor.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => runner.Calls == 1);

        await processor.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        processor.ExecuteTask.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task Processor_ShouldPollAgainImmediately_WhileBatchesComeBackFull()
    {
        var runner = new ScriptedRunner(call => call < 3 ? 5 : 0);
        var processor = NewProcessor(runner, Options(TimeSpan.FromHours(1)));

        await processor.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => runner.Calls >= 3);
        await Task.Delay(100);

        runner.Calls.ShouldBe(3);
        await processor.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Processor_ShouldKeepRunning_WhenACycleThrows()
    {
        var runner = new ScriptedRunner(call => call == 1 ? throw new InvalidOperationException("db is down") : 0);
        var processor = NewProcessor(runner, Options(TimeSpan.FromMilliseconds(20)));

        await processor.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => runner.Calls >= 3);

        runner.Calls.ShouldBeGreaterThanOrEqualTo(3);
        processor.ExecuteTask.IsCompleted.ShouldBeFalse();
        await processor.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Processor_ShouldNotStartACycle_WhenAlreadyStopping()
    {
        var runner = new ScriptedRunner(_ => 0);
        var processor = NewProcessor(runner, Options(TimeSpan.FromHours(1)));
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        await processor.StartAsync(stopping.Token);
        await Task.Delay(100);

        runner.Calls.ShouldBe(0);
        await processor.StopAsync(CancellationToken.None);
    }

    private sealed class ScriptedRunner(Func<int, int> script) : IOutboxBatchRunner
    {
        private int _calls;

        public int Calls => _calls;

        public Task<int> RunOnceAsync(CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref _calls);
            return Task.FromResult(script(call));
        }
    }
}
