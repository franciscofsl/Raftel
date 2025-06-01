using Raftel.Application.Authorization;
using Raftel.Application.Queries;

namespace Raftel.Demo.Application.Pirates.GetPirateByFilter;

[RequiresPermission(PiratesPermissions.View)]
public sealed record GetPirateByFilterQuery(string Name, int? MaxBounty) : IQuery<GetPirateByFilterResponse>;