using System.Runtime.Serialization;
using Raftel.Blazor.Shared.Grpc.Filters;

namespace Raftel.Demo.Application.Contracts.Samples;

[DataContract]
public class SampleFilterDto : GridFilter
{
}