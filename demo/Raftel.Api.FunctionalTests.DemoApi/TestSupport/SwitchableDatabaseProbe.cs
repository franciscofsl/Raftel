using Raftel.Infrastructure.Health;

namespace Raftel.Api.FunctionalTests.DemoApi.TestSupport;

public sealed class SwitchableDatabaseProbe(IDatabaseProbe inner, DatabaseFailureSwitch failureSwitch) : IDatabaseProbe
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        return failureSwitch.IsEnabled
            ? Task.FromResult(false)
            : inner.CanConnectAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken)
    {
        return inner.GetPendingMigrationsAsync(cancellationToken);
    }
}
