namespace Raftel.Api.Integration.Tests.Api.Application.Pirates.GetPirateById;

public sealed class GetPirateByIdResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; }
    
    public int Bounty { get; set; }
}