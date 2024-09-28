using Raftel.Core.Contracts;
using Raftel.Data.Tests.DbContext;
using Raftel.Data.Tests.Types.Models;

namespace Raftel.Data.Tests;

public class AuditingTestBase(TestingDbFixture fixture) : DataTestBase(fixture)
{
    [Fact]
    public async Task Should_Save_Creation_Audit()
    {
        var repository = GetRequiredService<IRepository<SampleAggregate, SampleId>>();

        var aggregate = SampleAggregate.Create();
        
        await repository.InsertAsync(aggregate);
    }
}