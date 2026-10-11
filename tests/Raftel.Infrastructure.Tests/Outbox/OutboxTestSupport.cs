using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Raftel.Application;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.Events;

namespace Raftel.Infrastructure.Tests.Outbox;

/// <summary>A clock tests can move forward to cross retry delays, lease expiry and retention cut-offs.</summary>
public sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public TestTimeProvider(DateTimeOffset start)
    {
        _now = start;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

/// <summary>What the test handlers observed while delivering events.</summary>
public sealed class HandlerLog
{
    private readonly ConcurrentQueue<HandlerCall> _calls = new();

    /// <summary>Makes <see cref="RecordingHandler"/> throw for the events this matches.</summary>
    public Func<PirateCrownedKing, bool> FailWhen { get; set; } = _ => false;

    /// <summary>Makes <see cref="SecondHandler"/> throw while set.</summary>
    public bool SecondHandlerFails { get; set; }

    /// <summary>How many more follow-up pirates <see cref="FollowUpHandler"/> should crown.</summary>
    public int FollowUpsLeft { get; set; }

    /// <summary>Cancelled by <see cref="BlockingHandler"/> to simulate a host shutdown mid-delivery.</summary>
    public CancellationTokenSource Shutdown { get; set; } = new();

    public IReadOnlyList<HandlerCall> Calls => _calls.ToArray();

    public void Record(HandlerCall call) => _calls.Enqueue(call);

    public IReadOnlyList<HandlerCall> CallsOf(string handler) => Calls.Where(c => c.Handler == handler).ToList();
}

public sealed record HandlerCall(
    string Handler,
    PirateCrownedKing Event,
    Guid? Tenant,
    string CorrelationId);

/// <summary>Records every event it receives, with the ambient tenant and correlation id it ran under.</summary>
public sealed class RecordingHandler(HandlerLog log, ICurrentTenant tenant, ICorrelationContext correlation)
    : IDomainEventHandler<PirateCrownedKing>
{
    public Task HandleAsync(PirateCrownedKing domainEvent, CancellationToken cancellationToken = default)
    {
        log.Record(new HandlerCall(nameof(RecordingHandler), domainEvent, tenant.Id, correlation.CorrelationId));

        return log.FailWhen(domainEvent)
            ? throw new InvalidOperationException("selective failure")
            : Task.CompletedTask;
    }
}

/// <summary>A second handler for the same event, to exercise partial failure across handlers.</summary>
public sealed class SecondHandler(HandlerLog log, ICurrentTenant tenant, ICorrelationContext correlation)
    : IDomainEventHandler<PirateCrownedKing>
{
    public Task HandleAsync(PirateCrownedKing domainEvent, CancellationToken cancellationToken = default)
    {
        log.Record(new HandlerCall(nameof(SecondHandler), domainEvent, tenant.Id, correlation.CorrelationId));

        return log.SecondHandlerFails
            ? throw new InvalidOperationException("second handler failed")
            : Task.CompletedTask;
    }
}

/// <summary>Writes a pirate through the unit of work, then fails: the write must not survive.</summary>
public sealed class WriteThenThrowHandler(IPirateRepository pirates, IUnitOfWork unitOfWork)
    : IDomainEventHandler<PirateCrownedKing>
{
    public async Task HandleAsync(PirateCrownedKing domainEvent, CancellationToken cancellationToken = default)
    {
        await pirates.AddAsync(Pirate.Normal("Ghost", 1), cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        throw new InvalidOperationException("failed after writing");
    }
}

/// <summary>Writes a pirate through the unit of work, so the tenant it ran under can be checked.</summary>
public sealed class TenantWritingHandler(IPirateRepository pirates, IUnitOfWork unitOfWork)
    : IDomainEventHandler<PirateCrownedKing>
{
    public async Task HandleAsync(PirateCrownedKing domainEvent, CancellationToken cancellationToken = default)
    {
        await pirates.AddAsync(Pirate.Normal("FromHandler", 1), cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}

/// <summary>Crowns another pirate from inside a handler, raising a further domain event.</summary>
public sealed class FollowUpHandler(HandlerLog log, IPirateRepository pirates, IUnitOfWork unitOfWork)
    : IDomainEventHandler<PirateCrownedKing>
{
    public async Task HandleAsync(PirateCrownedKing domainEvent, CancellationToken cancellationToken = default)
    {
        if (log.FollowUpsLeft <= 0)
        {
            return;
        }

        log.FollowUpsLeft--;
        var pirate = Pirate.Normal("Follow-up", 1);
        pirate.FoundOnePiece();
        await pirates.AddAsync(pirate, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}

/// <summary>Triggers the host shutdown and then waits for it, like a handler stuck in a slow call.</summary>
public sealed class BlockingHandler(HandlerLog log) : IDomainEventHandler<PirateCrownedKing>
{
    public async Task HandleAsync(PirateCrownedKing domainEvent, CancellationToken cancellationToken = default)
    {
        await log.Shutdown.CancelAsync();
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}

/// <summary>Always fails.</summary>
public sealed class ThrowingHandler(HandlerLog log, ICurrentTenant tenant, ICorrelationContext correlation)
    : IDomainEventHandler<PirateCrownedKing>
{
    public Task HandleAsync(PirateCrownedKing domainEvent, CancellationToken cancellationToken = default)
    {
        log.Record(new HandlerCall(nameof(ThrowingHandler), domainEvent, tenant.Id, correlation.CorrelationId));
        throw new InvalidOperationException("handler exploded");
    }
}

public abstract class OutboxTestBase : InfrastructureTestBase
{
    protected static readonly DateTimeOffset Start = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    protected OutboxTestBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected TestTimeProvider Clock { get; } = new(Start);

    protected HandlerLog Log { get; } = new();

    protected sealed override Action<OutboxOptions> ConfigureOutbox => options =>
    {
        options.Enabled = true;
        ConfigureOptions(options);
    };

    protected virtual void ConfigureOptions(OutboxOptions options)
    {
    }

    /// <summary>Whether the demo assembly's own handlers are discovered, making the event "handled".</summary>
    protected virtual bool ScanDemoHandlers => true;

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddRaftelApplication(cfg =>
        {
            if (ScanDemoHandlers)
            {
                cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommand).Assembly);
            }
        });

        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton(Log);
    }
}

public sealed record LogEntry(LogLevel Level, string Category, string Message, IReadOnlyDictionary<string, object> State);

/// <summary>Captures log entries with their structured state so tests can assert on the fields.</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries.ToArray();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var fields = (state as IEnumerable<KeyValuePair<string, object>>)?
                .ToDictionary(pair => pair.Key, pair => pair.Value) ?? new Dictionary<string, object>();

            entries.Enqueue(new LogEntry(logLevel, category, formatter(state, exception), fields));
        }
    }
}
