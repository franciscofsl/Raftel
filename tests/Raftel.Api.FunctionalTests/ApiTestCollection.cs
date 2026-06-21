namespace Raftel.Api.FunctionalTests;

[CollectionDefinition(Name)]
public sealed class ApiTestCollection : ICollectionFixture<ApiTestFactory>
{
    public const string Name = "Api collection";
}
