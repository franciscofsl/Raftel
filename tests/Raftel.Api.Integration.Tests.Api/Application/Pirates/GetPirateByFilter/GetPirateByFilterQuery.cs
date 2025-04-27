using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Api.Integration.Tests.Api.Application.Pirates.GetPirateByFilter;
public sealed record GetPirateByFilterQuery(string Name, int? MaxBounty) : IQuery<GetPirateByFilterResponse>;