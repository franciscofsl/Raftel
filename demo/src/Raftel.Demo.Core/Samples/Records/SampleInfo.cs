namespace Raftel.Demo.Core.Samples.Records;

public record SampleInfo
{
    public Guid Id { get; init; }
    public string Code { get; init; }
    public string Name { get; init; }
    public string ColorName { get; init; }
    public string HexadecimalColor { get; init; }
    public string LotNumber { get; init; }
    public string Record { get; init; }
    public DateTime? ExpirationDate { get; init; }
}