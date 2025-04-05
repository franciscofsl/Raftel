using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Raftel.Application.Queries;

namespace Raftel.Application.Tests.Queries;

public class QueryDispatcherTests
{
    public class GetProductQuery : IQuery<Product>
    {
        public int ProductId { get; set; }
    }

    public   class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    [Fact]
    public async Task DispatchAsync_Should_ReturnResult_When_QueryIsDispatched()
    {
        var mockHandler = Substitute.For<IQueryHandler<GetProductQuery, Product>>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IQueryHandler<GetProductQuery, Product>)).Returns(mockHandler);

        var queryDispatcher = new QueryDispatcher(serviceProvider);
        var query = new GetProductQuery { ProductId = 1 };

        var expectedProduct = new Product { Id = 1, Name = "Sample Product", Price = 20.0m };
        mockHandler.HandleAsync(query).Returns(Task.FromResult(expectedProduct));

        var result = await queryDispatcher.DispatchAsync<GetProductQuery, Product>(query);

        Assert.Equal(expectedProduct, result);
        await mockHandler.Received(1).HandleAsync(query);
    }

    [Fact]
    public async Task DispatchAsync_Should_ThrowException_When_QueryHandlerFails()
    {
        var mockHandler = Substitute.For<IQueryHandler<GetProductQuery, Product>>();
        mockHandler.HandleAsync(Arg.Any<GetProductQuery>())
            .Throws(new Exception("Handler failed"));

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IQueryHandler<GetProductQuery, Product>)).Returns(mockHandler);

        var queryDispatcher = new QueryDispatcher(serviceProvider);
        var query = new GetProductQuery { ProductId = 1 };

        await Assert.ThrowsAsync<Exception>(() => queryDispatcher.DispatchAsync<GetProductQuery, Product>(query));
    }
}