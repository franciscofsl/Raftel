namespace Raftel.Infrastructure.Tests;

[CollectionDefinition(Name)]
public sealed class PostgreSqlTestCollection : ICollectionFixture<PostgreSqlTestContainerFixture>
{
    public const string Name = "PostgreSql collection";
}
