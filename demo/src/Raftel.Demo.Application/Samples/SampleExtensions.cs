using Raftel.Demo.Application.Contracts.Samples;
using Raftel.Demo.Core.Samples;

namespace Raftel.Demo.Application.Samples;

public static class SampleExtensions
{
    public static SampleDto ToDto(this Sample sample)
    {
        return new SampleDto
        {
            Id = sample.Id
        };
    }
}