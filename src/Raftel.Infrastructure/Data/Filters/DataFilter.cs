using Raftel.Shared;

namespace Raftel.Infrastructure.Data.Filters;

internal class DataFilter : IDataFilter
{
    private static readonly AsyncLocal<HashSet<Type>> DisabledFilters = new();

    public bool IsEnabled<TFilter>()
    {
        var set = DisabledFilters.Value;
        return set == null || !set.Contains(typeof(TFilter));
    }

    public IDisposable Disable<TFilter>()
    {
        var set = DisabledFilters.Value ??= new HashSet<Type>();
        set.Add(typeof(TFilter));
        return new DisposeAction(() => set.Remove(typeof(TFilter)));
    }
}