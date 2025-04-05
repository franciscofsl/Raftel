using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Configuration;
using Raftel.Inkventory.Infrastructure;

namespace Raftel.Inkventory.Data.Tests
{
    public abstract class InkventoryDataTestBase : IClassFixture<SqlServerTestContainerFixture>, IAsyncDisposable
    {
        protected readonly InkventoryDbContext DbContext;
        private readonly SqlServerTestContainerFixture _fixture;
        private readonly ServiceProvider _serviceProvider;

        protected InkventoryDataTestBase(SqlServerTestContainerFixture fixture)
        {
            _fixture = fixture;

            var options = new DbContextOptionsBuilder<InkventoryDbContext>()
                .UseSqlServer(_fixture.ConnectionString)
                .Options;

            DbContext = new InkventoryDbContext(options);
            DbContext.Database.EnsureCreated();

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", _fixture.ConnectionString)
            });

            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureInkventoryData(configurationBuilder.Build());
            serviceCollection.ConfigureInkventoryInfrastructure();
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        protected T GetService<T>()
        {
            return _serviceProvider.GetRequiredService<T>();
        }
 
        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _serviceProvider.DisposeAsync();
        }
    }
}