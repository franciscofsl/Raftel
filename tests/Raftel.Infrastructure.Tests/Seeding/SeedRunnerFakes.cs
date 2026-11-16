using Microsoft.Extensions.Logging;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Raftel.Application;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Raftel.Infrastructure.Seeding;

namespace Raftel.Infrastructure.Tests.Seeding;

internal sealed class SeedLog
{
    public List<string> Entries { get; } = new();
    public void Add(string entry) => Entries.Add(entry);
}

internal sealed class FakeTransaction(SeedLog log) : ITransaction
{
    private bool _completed;

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        _completed = true;
        log.Add("tx.commit");
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        _completed = true;
        log.Add("tx.rollback");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            log.Add("tx.disposed-uncompleted");
        }

        return ValueTask.CompletedTask;
    }
}

internal sealed class FakeUnitOfWork(SeedLog log) : IUnitOfWork
{
    public bool HasActiveTransaction => false;
    public Task CommitAsync(CancellationToken cancellationToken = default) { log.Add("uow.commit"); return Task.CompletedTask; }
    public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        log.Add("tx.begin");
        return Task.FromResult<ITransaction>(new FakeTransaction(log));
    }
}

internal sealed class InMemorySeedHistoryStore : ISeedHistoryStore
{
    public Dictionary<(string Name, Guid? TenantId), int> Applied { get; } = new();

    public Task<int?> GetAppliedVersionAsync(string name, Guid? tenantId, CancellationToken cancellationToken) =>
        Task.FromResult<int?>(Applied.TryGetValue((name, tenantId), out var version) ? version : null);

    public Task RecordAsync(string name, Guid? tenantId, int version, CancellationToken cancellationToken)
    {
        Applied[(name, tenantId)] = version;
        return Task.CompletedTask;
    }
}

internal sealed class FakeCurrentTenant : ICurrentTenant
{
    private Guid? _id;
    public Guid? Id => _id;

    public IDisposable Change(Guid? tenantId)
    {
        var previous = _id;
        _id = tenantId;
        return new Restore(() => _id = previous);
    }

    private sealed class Restore(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}

internal class ScriptedSeeder(
    SeedLog log,
    string name,
    int order,
    Func<Task<Result>>? action = null,
    bool runOnce = false,
    int version = 1) : IDataSeeder
{
    public int Order => order;
    public string Name => name;
    public bool RunOnce => runOnce;
    public int Version => version;

    public async Task<Result> SeedAsync(CancellationToken cancellationToken = default)
    {
        log.Add($"seed:{name}");
        return action is null ? Result.Success() : await action();
    }
}

internal sealed class ScopeMarker
{
    public Guid Id { get; } = Guid.NewGuid();
}

internal sealed class ScriptedTenantSeeder(
    SeedLog log,
    FakeCurrentTenant currentTenant,
    ScopeMarker marker,
    string name,
    int order,
    Func<TenantId, Task<Result>>? action = null) : ITenantDataSeeder
{
    public int Order => order;
    public string Name => name;

    public async Task<Result> SeedForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        log.Add($"seed:{name}:{(Guid)tenantId}:current={currentTenant.Id}:scope={marker.Id}");
        return action is null ? Result.Success() : await action(tenantId);
    }
}

internal sealed class SeedRunnerHarness
{
    private readonly ServiceCollection _services = new();

    public SeedRunnerHarness(SeedOptions? options = null, IEnumerable<Tenant>? tenants = null)
    {
        Log = new SeedLog();
        History = new InMemorySeedHistoryStore();
        CurrentTenant = new FakeCurrentTenant();

        _services.AddSingleton(Log);
        _services.AddSingleton(History);
        _services.AddSingleton<ISeedHistoryStore>(History);
        _services.AddSingleton(CurrentTenant);
        _services.AddSingleton<ICurrentTenant>(CurrentTenant);
        _services.AddSingleton<ILogger<SeedRunner>>(NullLogger<SeedRunner>.Instance);
        _services.AddSingleton<IOptions<SeedOptions>>(Options.Create(options ?? new SeedOptions()));
        _services.AddScoped<IUnitOfWork, FakeUnitOfWork>();
        _services.AddScoped<ScopeMarker>();

        var tenantsRepository = Substitute.For<ITenantsRepository>();
        tenantsRepository
            .ListAllAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((tenants ?? []).ToList()));
        _services.AddSingleton(tenantsRepository);

        _services.AddScoped<ISeedRunner, SeedRunner>();
    }

    public SeedLog Log { get; }
    public InMemorySeedHistoryStore History { get; }
    public FakeCurrentTenant CurrentTenant { get; }

    public SeedRunnerHarness With(Func<SeedLog, IDataSeeder> factory)
    {
        _services.AddScoped<IDataSeeder>(_ => factory(Log));
        return this;
    }

    public SeedRunnerHarness WithTenantSeeder(string name, int order, Func<TenantId, Task<Result>>? action = null)
    {
        _services.AddScoped<IDataSeeder>(sp => new ScriptedTenantSeeder(Log, CurrentTenant, sp.GetRequiredService<ScopeMarker>(), name, order, action));
        return this;
    }

    public async Task<Result> RunAsync()
    {
        using var provider = _services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISeedRunner>().RunAsync();
    }

    public Guid AmbientScopeId { get; private set; }

    public async Task<Result> RunForTenantAsync(TenantId tenantId)
    {
        using var provider = _services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        AmbientScopeId = scope.ServiceProvider.GetRequiredService<ScopeMarker>().Id;
        return await scope.ServiceProvider.GetRequiredService<ISeedRunner>().RunForTenantAsync(tenantId);
    }
}
