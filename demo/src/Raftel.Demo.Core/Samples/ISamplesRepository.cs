using Raftel.Core.Contracts;
using Raftel.Demo.Core.Samples.ValueObjects;

namespace Raftel.Demo.Core.Samples;

public interface ISamplesRepository : IRepository<Sample, SampleId>
{
}