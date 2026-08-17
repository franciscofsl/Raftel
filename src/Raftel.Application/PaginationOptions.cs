using Raftel.Domain.Abstractions;

namespace Raftel.Application;

/// <summary>
/// Configures the default and maximum page size applied by paged queries when the
/// client does not request a page size, or requests one above the configured maximum.
/// </summary>
public sealed class PaginationOptions
{
    /// <summary>
    /// Gets or sets the page size used when a paged query does not specify one.
    /// </summary>
    public int DefaultPageSize { get; set; } = PageRequest.DefaultPageSize;

    /// <summary>
    /// Gets or sets the largest page size a paged query may request.
    /// </summary>
    public int MaxPageSize { get; set; } = PageRequest.MaxPageSize;
}
