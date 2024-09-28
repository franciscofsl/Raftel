using Raftel.Core.BaseTypes;

namespace Raftel.Data.Tests.Types.Models;

public class SampleNotAuditedAggregate : AggregateRoot<SampleId>
{
    private SampleNotAuditedAggregate()
    {
    }

    public bool Processed { get; set; }

    public static SampleNotAuditedAggregate Create()
    {
        var sample = new SampleNotAuditedAggregate
        {
            Id = EntityIdGenerator.Create<SampleId>()
        };

        sample.RaiseDomainEvent(new SampleModelCreated
        {
            Id = sample.Id
        });

        return sample;
    }
}