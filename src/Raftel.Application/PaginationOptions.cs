using Raftel.Domain.Abstractions;

namespace Raftel.Application;

public sealed class PaginationOptions
{
    public int DefaultPageSize { get; set; } = PageRequest.DefaultPageSize;

    public int MaxPageSize { get; set; } = PageRequest.MaxPageSize;
}
