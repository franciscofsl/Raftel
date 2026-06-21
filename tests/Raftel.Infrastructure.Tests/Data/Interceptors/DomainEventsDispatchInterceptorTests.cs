using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.Events;
using Raftel.Domain.Abstractions;

namespace Raftel.Infrastructure.Tests.Data.Interceptors;

[Collection(SqlServerTestCollection.Name)]
public class DomainEventsDispatchInterceptorTests : InfrastructureTestBase
{
    private readonly RecordingDomainEventsDispatcher _dispatcher = new();

    public DomainEventsDispatchInterceptorTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddSingleton<IDomainEventsDispatcher>(_dispatcher);
    }

    [Fact]
    public async Task CommitAsync_ShouldDispatchDomainEvents_RaisedByTrackedAggregates()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPirateRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            var pirate = Pirate.Normal("Luffy", 150_000_000);
            pirate.FoundOnePiece();

            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            var dispatched = _dispatcher.DispatchedEvents.OfType<PirateCrownedKing>().ShouldHaveSingleItem();
            dispatched.PirateId.ShouldBe(pirate.Id);
        });
    }

    [Fact]
    public async Task CommitAsync_ShouldClearDomainEvents_AfterDispatching()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPirateRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            var pirate = Pirate.Normal("Luffy", 150_000_000);
            pirate.FoundOnePiece();

            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            pirate.DomainEvents.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task CommitAsync_ShouldNotDispatchAnyEvent_WhenAggregateRaisesNone()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPirateRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            var pirate = Pirate.Normal("Zoro", 120_000_000);

            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            _dispatcher.DispatchedEvents.ShouldBeEmpty();
        });
    }

    private sealed class RecordingDomainEventsDispatcher : IDomainEventsDispatcher
    {
        private readonly List<IDomainEvent> _dispatchedEvents = new();

        public IReadOnlyCollection<IDomainEvent> DispatchedEvents => _dispatchedEvents;

        public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events,
            CancellationToken cancellationToken = default)
        {
            _dispatchedEvents.AddRange(events);
            return Task.CompletedTask;
        }
    }
}
