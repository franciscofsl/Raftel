namespace Raftel.Domain.Abstractions;

public interface IPagedResult
{
    long TotalCount { get; }

    int TotalPages { get; }
}
