using Raftel.Domain.Abstractions;

namespace Raftel.Application.Queries;

public interface IPagedQuery<TItem> : IQuery<PagedResult<TItem>>
{
    int? Page { get; }

    int? PageSize { get; }

    string Sort { get; }
}
