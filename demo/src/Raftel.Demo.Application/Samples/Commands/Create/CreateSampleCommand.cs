using Raftel.Application.Cqrs.Commands;
using Raftel.Demo.Application.Contracts.Samples;
using Raftel.Shared.Results;

namespace Raftel.Demo.Application.Samples.Commands.Create;

public record CreateSampleCommand : ICommand<Result<SampleDto>>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string ColorName { get; set; }
    public string HexadecimalColor { get; set; }
    public string LotNumber { get; set; }
    public string Record { get; set; }
    public DateTime? ExpirationDate { get; set; }
}