using Raftel.Core.BaseTypes;
using Raftel.Demo.Core.Samples.ValueObjects;

namespace Raftel.Demo.Core.Samples;

public sealed class Sample : AggregateRoot<SampleId>
{
    private Sample()
    {
    }

    public static Sample Create()
    {
        return new Sample
        {
            Id = EntityIdGenerator.Create<SampleId>()
        };
    }
}