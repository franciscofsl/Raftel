using System.Runtime.Serialization;

namespace Raftel.Demo.Application.Contracts.Samples;

[DataContract]
public class SampleDto
{
    [DataMember(Order = 1)] public Guid Id { get; set; }
}