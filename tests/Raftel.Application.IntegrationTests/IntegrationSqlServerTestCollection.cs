using Raftel.Infrastructure.Tests;

namespace Raftel.Application.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class IntegrationSqlServerTestCollection : ICollectionFixture<SqlServerTestContainerFixture>
{
    public const string Name = "Integration SqlServer collection";
}
