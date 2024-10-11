using System.Runtime.Serialization;

namespace Raftel.Demo.Application.Contracts.Samples;

[DataContract]
public class SampleForListDto
{
    [DataMember(Order = 1)] public Guid Id { get; init; }
}