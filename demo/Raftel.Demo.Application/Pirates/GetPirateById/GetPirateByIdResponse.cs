namespace Raftel.Demo.Application.Pirates.GetPirateById;

public sealed class GetPirateByIdResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; }
    
    public uint Bounty { get; set; }
} 