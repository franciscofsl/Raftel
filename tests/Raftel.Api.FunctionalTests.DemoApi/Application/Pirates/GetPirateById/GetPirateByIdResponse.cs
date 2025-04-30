namespace Raftel.Api.FunctionalTests.DemoApi.Application.Pirates.GetPirateById;

public sealed class GetPirateByIdResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; }
    
    public int Bounty { get; set; }
}