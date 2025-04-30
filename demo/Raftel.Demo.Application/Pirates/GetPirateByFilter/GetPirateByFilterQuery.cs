using Raftel.Application.Queries;

namespace Raftel.Demo.Application.Pirates.GetPirateByFilter;

public sealed record GetPirateByFilterQuery(string Name, int? MaxBounty) : IQuery<GetPirateByFilterResponse>;