using FluentAssertions;
using NSubstitute;
using Quartz;
using Raftel.Core.Contracts;
using Raftel.Data.Tests.DbContext;
using Raftel.Data.Tests.Types.Models;
using Raftel.Infrastructure.BackgroundJobs;

namespace Raftel.Data.Tests;

public class RaftelDbContextTestBase : DataTestBase
{
    public RaftelDbContextTestBase(TestingDbFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Should_Dispatch_Domain_Events_With_Outbox()
    {
        var repository = GetRequiredService<IRepository<SampleAggregate, SampleId>>();

        var aggregate = SampleAggregate.Create();

        aggregate.Processed.Should().BeFalse();

        await repository.InsertAsync(aggregate);

        var processor = GetRequiredService<ProcessOutboxMessagesJob>();

        var mock = Substitute.For<IJobExecutionContext>();
        mock.CancellationToken.Returns(new CancellationToken());

        await processor.Execute(mock);
        await Task.Delay(1000);
        var createdAggregate = await repository.GetAsync(aggregate.Id);

        createdAggregate.Processed.Should().BeTrue();
    }
}