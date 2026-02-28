using Raftel.Application.Authorization;
using Raftel.Application.Queries;

namespace Raftel.Demo.Application.Pirates.GetPiratesPaged;

[RequiresPermission(PiratesPermissions.View)]
public sealed record GetPiratesPagedQuery(int Page, int PageSize, string? Name) : IPagedQuery<PiratePageInfo>;
