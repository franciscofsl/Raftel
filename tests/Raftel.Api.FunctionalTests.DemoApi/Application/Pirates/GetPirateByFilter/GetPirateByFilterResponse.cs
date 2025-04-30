namespace Raftel.Api.FunctionalTests.DemoApi.Application.Pirates.GetPirateByFilter;
public sealed class GetPirateByFilterResponse
{
    public List<PirateInfo> Pirates { get; set; }
}

public sealed record PirateInfo(string Name, int Bounty);