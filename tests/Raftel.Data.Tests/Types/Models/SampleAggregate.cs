﻿using Raftel.Core.Attributes;
using Raftel.Core.BaseTypes;

namespace Raftel.Data.Tests.Types.Models;

[Auditable]
public class SampleAggregate : AggregateRoot<SampleId>
{
    private SampleAggregate()
    {
    }

    public bool Processed { get; set; }
    
    public string StringValue { get; set; }
    public int IntegerValue { get; set; }

    public static SampleAggregate Create()
    {
        var sample = new SampleAggregate
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