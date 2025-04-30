using Raftel.Application.Queries;

namespace Raftel.Tests.Common.Application.Pirates.GetPirateByFilter;

public sealed record GetPirateByFilterQuery(string Name, int? MaxBounty) : IQuery<GetPirateByFilterResponse>;