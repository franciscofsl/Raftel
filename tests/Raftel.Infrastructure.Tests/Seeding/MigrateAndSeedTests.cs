using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raftel.Application;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Domain.Abstractions;
using Raftel.Infrastructure.Health;
using Raftel.Infrastructure.Seeding;

namespace Raftel.Infrastructure.Tests.Seeding;

public class MigrateAndSeedTests
{
    private static readonly Error Boom = Error.Failure("Test.Boom", "boom");

    private readonly ISeedRunner _runner = Substitute.For<ISeedRunner>();
    private readonly IDatabaseMigrator _migrator = Substitute.For<IDatabaseMigrator>();
    private readonly IDatabaseProbe _probe = Substitute.For<IDatabaseProbe>();
    private readonly SeedingState _state = new();
    private readonly SeedOptions _options = new();

    public MigrateAndSeedTests()
    {
        _runner.RunAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _probe.GetPendingMigrationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<string>>([]));
    }

    private IHost BuildHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_state);
        services.AddSingleton<IOptions<SeedOptions>>(Options.Create(_options));
        services.AddScoped(_ => _runner);
        services.AddScoped(_ => _migrator);
        services.AddScoped(_ => _probe);
        return new TestHost(services.BuildServiceProvider());
    }

    [Fact]
    public async Task MigrateAndSeedAsync_WithMigrationEnabled_ShouldMigrateBeforeAnySeeder()
    {
        _options.ApplyMigrationsOnStartup = true;

        await BuildHost().MigrateAndSeedAsync();

        Received.InOrder(() =>
        {
            _migrator.MigrateAsync(Arg.Any<CancellationToken>());
            _runner.RunAsync(Arg.Any<CancellationToken>());
        });
        _state.Status.ShouldBe(SeedingStatus.Completed);
    }

    [Fact]
    public async Task MigrateAndSeedAsync_WithMigrationDisabledAndPendingMigrations_ShouldFailWithoutRunningSeeders()
    {
        _probe.GetPendingMigrationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<string>>(["20260101_Initial"]));

        var exception = await Should.ThrowAsync<SeedingFailedException>(() => BuildHost().MigrateAndSeedAsync());

        exception.Message.ShouldContain("pending");
        await _migrator.DidNotReceive().MigrateAsync(Arg.Any<CancellationToken>());
        await _runner.DidNotReceive().RunAsync(Arg.Any<CancellationToken>());
        _state.Status.ShouldBe(SeedingStatus.Failed);
    }

    [Fact]
    public async Task MigrateAndSeedAsync_WithMigrationDisabledAndNoPendingMigrations_ShouldRunSeeders()
    {
        await BuildHost().MigrateAndSeedAsync();

        await _migrator.DidNotReceive().MigrateAsync(Arg.Any<CancellationToken>());
        await _runner.Received(1).RunAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MigrateAndSeedAsync_WithSeedersDisabled_ShouldSkipSeedersButStillMigrate()
    {
        _options.RunSeedersOnStartup = false;
        _options.ApplyMigrationsOnStartup = true;

        await BuildHost().MigrateAndSeedAsync();

        await _migrator.Received(1).MigrateAsync(Arg.Any<CancellationToken>());
        await _runner.DidNotReceive().RunAsync(Arg.Any<CancellationToken>());
        _state.Status.ShouldBe(SeedingStatus.Completed);
    }

    [Fact]
    public async Task MigrateAndSeedAsync_WhenSeedingFailsWithFailFast_ShouldThrow()
    {
        _runner.RunAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(Boom));

        var exception = await Should.ThrowAsync<SeedingFailedException>(() => BuildHost().MigrateAndSeedAsync());

        exception.Error.ShouldBe(Boom);
        _state.Status.ShouldBe(SeedingStatus.Failed);
    }

    [Fact]
    public async Task MigrateAndSeedAsync_WhenSeedingFailsWithoutFailFast_ShouldStartAndReportDegraded()
    {
        _options.FailFastOnError = false;
        _runner.RunAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(Boom));

        await BuildHost().MigrateAndSeedAsync();

        _state.Status.ShouldBe(SeedingStatus.CompletedWithFailures);
    }

    [Fact]
    public async Task MigrateAndSeedAsync_WhenAnUnexpectedExceptionIsThrown_ShouldMarkFailedAndRethrow()
    {
        _runner.RunAsync(Arg.Any<CancellationToken>()).Returns<Result>(_ => throw new InvalidOperationException("db down"));

        await Should.ThrowAsync<InvalidOperationException>(() => BuildHost().MigrateAndSeedAsync());

        _state.Status.ShouldBe(SeedingStatus.Failed);
    }

    private sealed class TestHost(IServiceProvider services) : IHost
    {
        public IServiceProvider Services => services;
        public void Dispose() { }
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
