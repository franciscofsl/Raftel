namespace Raftel.Api.Integration.Tests.Api.Application.Pirates.GetPirateById;
public sealed record GetPirateByIdQuery(Guid Id, string Name, int? MaxBounty) : IQuery<GetPirateByIdResponse>;