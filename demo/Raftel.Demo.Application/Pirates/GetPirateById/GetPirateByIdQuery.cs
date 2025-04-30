using Raftel.Application.Queries;

namespace Raftel.Demo.Application.Pirates.GetPirateById;
public sealed record GetPirateByIdQuery(Guid Id, string Name, int? MaxBounty) : IQuery<GetPirateByIdResponse>;