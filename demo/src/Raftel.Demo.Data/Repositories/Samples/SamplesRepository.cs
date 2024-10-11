using Raftel.Data.DbContexts;
using Raftel.Data.Repositories;
using Raftel.Demo.Core.Samples;
using Raftel.Demo.Core.Samples.ValueObjects;

namespace Raftel.Demo.Data.Repositories.Samples;

public class SamplesRepository : EfRepository<Sample, SampleId>, ISamplesRepository
{
    public SamplesRepository(IDbContextFactory context) : base(context)
    {
    }
}