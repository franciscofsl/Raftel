using Raftel.Infrastructure.Data;
using Respawn;

namespace Raftel.Infrastructure.Tests;

public interface IDbContainerFixture : IAsyncLifetime
{
    string ConnectionString { get; }

    DatabaseProvider Provider { get; }

    IDbAdapter RespawnAdapter { get; }
}
