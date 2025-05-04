namespace Raftel.Shared;

public class DisposeAction : IDisposable
{
    private readonly Action _onDispose;
    public DisposeAction(Action onDispose) => _onDispose = onDispose;
    
    public void Dispose() => _onDispose();
}