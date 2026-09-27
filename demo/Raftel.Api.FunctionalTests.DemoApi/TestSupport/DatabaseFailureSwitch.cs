namespace Raftel.Api.FunctionalTests.DemoApi.TestSupport;

public sealed class DatabaseFailureSwitch
{
    private volatile bool _isEnabled;

    public bool IsEnabled => _isEnabled;

    public void Enable() => _isEnabled = true;

    public void Disable() => _isEnabled = false;
}
