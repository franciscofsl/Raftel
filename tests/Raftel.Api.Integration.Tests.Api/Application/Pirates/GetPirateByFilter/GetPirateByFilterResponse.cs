namespace Raftel.Api.Integration.Tests.Api.Application.Pirates.GetPirateByFilter;
public sealed class GetPirateByFilterResponse
{
    public List<PirateInfo> Pirates { get; set; }
}

public sealed record PirateInfo(string Name, int Bounty);