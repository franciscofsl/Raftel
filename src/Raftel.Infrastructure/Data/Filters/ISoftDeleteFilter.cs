using Raftel.Shared.Attributes;

namespace Raftel.Infrastructure.Data.Filters;

/// <summary>
/// Marker interface used to identify the soft delete filter in the data filtering system.
/// </summary>
/// <remarks>
/// This interface has no members and is used exclusively as a type token to represent the
/// soft delete filter when enabling or disabling it at runtime via <see cref="IDataFilter"/>.
/// </remarks>
[Marker]
public interface ISoftDeleteFilter
{
}
