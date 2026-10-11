namespace Raftel.Application.Abstractions.Concurrency;

/// <summary>
/// Reads the opaque concurrency version of a version-tracked entity, without exposing any
/// persistence-framework type.
/// </summary>
public interface IEntityVersionReader
{
    /// <summary>
    /// Gets the current version of a tracked entity.
    /// </summary>
    /// <param name="entity">The entity instance, as loaded by the current unit of work.</param>
    /// <returns>
    /// The opaque version, or <see langword="null"/> when the entity is not version-tracked or is not
    /// tracked by the current unit of work.
    /// </returns>
    string VersionOf(object entity);
}
