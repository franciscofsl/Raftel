namespace Raftel.Infrastructure.Tests;

[CollectionDefinition(Name)]
public sealed class SqlServerTestCollection : ICollectionFixture<SqlServerTestContainerFixture>
{
    public const string Name = "SqlServer collection";
}
