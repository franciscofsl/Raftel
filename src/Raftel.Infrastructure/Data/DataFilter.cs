namespace Raftel.Infrastructure.Data;

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

    private class DisposeAction : IDisposable
    {
        private readonly Action _onDispose;
        public DisposeAction(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }
}