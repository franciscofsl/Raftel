namespace Raftel.Data.Tests.DbContext;

public class DataTestBase : IClassFixture<TestingDbFixture>
{
    private readonly TestingDbFixture _fixture;

    public DataTestBase(TestingDbFixture fixture)
    {
        _fixture = fixture;
    }

    protected TService GetRequiredService<TService>()
    {
        return _fixture.GetRequiredService<TService>();
    }
}