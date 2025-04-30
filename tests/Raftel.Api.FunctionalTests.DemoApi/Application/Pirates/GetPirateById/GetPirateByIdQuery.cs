namespace Raftel.Api.FunctionalTests.DemoApi.Application.Pirates.GetPirateById;
public sealed record GetPirateByIdQuery(Guid Id, string Name, int? MaxBounty) : IQuery<GetPirateByIdResponse>;