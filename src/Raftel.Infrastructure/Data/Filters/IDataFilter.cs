namespace Raftel.Infrastructure.Data.Filters;

/// <summary>
/// Provides a mechanism to enable or disable data-level filters dynamically at runtime,
/// such as soft delete or multi-tenancy filters. Filters are typically evaluated during
/// query execution to include or exclude entities based on application-specific rules.
/// </summary>
public interface IDataFilter
{
    /// <summary>
    /// Determines whether a given data filter is currently enabled in the active execution context.
    /// </summary>
    /// <typeparam name="TFilter">
    /// The marker type that identifies the filter (e.g., <c>ISoftDeleteFilter</c>).
    /// </typeparam>
    /// <returns>
    /// <c>true</c> if the specified filter is enabled and should be applied to queries;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool IsEnabled<TFilter>();

    /// <summary>
    /// Temporarily disables the specified data filter for the current asynchronous execution scope.
    /// When the returned <see cref="IDisposable"/> is disposed, the filter is automatically re-enabled.
    /// </summary>
    /// <typeparam name="TFilter">
    /// The marker type that identifies the filter to disable.
    /// </typeparam>
    /// <returns>
    /// An <see cref="IDisposable"/> handle that, when disposed, restores the filter to its previous state.
    /// </returns>
    IDisposable Disable<TFilter>();
}