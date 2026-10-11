using Microsoft.AspNetCore.Http;
using Raftel.Application.Abstractions;
using Raftel.Infrastructure.Correlation;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Correlation;

public class CorrelationScopeTests
{
    private readonly CorrelationScope _scope = new();

    private static CorrelationContext NewContext(string headerValue = null)
    {
        var httpContext = new DefaultHttpContext();
        if (headerValue is not null)
        {
            httpContext.Request.Headers["X-Correlation-Id"] = headerValue;
        }

        return new CorrelationContext(new HttpContextAccessor { HttpContext = httpContext });
    }

    [Fact]
    public void Begin_ShouldOverrideCorrelationId_UntilDisposed()
    {
        ICorrelationContext context = NewContext("from-request");

        using (_scope.Begin("abc"))
        {
            context.CorrelationId.ShouldBe("abc");
        }

        context.CorrelationId.ShouldBe("from-request");
    }

    [Fact]
    public void Begin_ShouldOverride_EvenAfterTheIdWasAlreadyResolved()
    {
        ICorrelationContext context = NewContext("from-request");
        _ = context.CorrelationId;

        using (_scope.Begin("abc"))
        {
            context.CorrelationId.ShouldBe("abc");
        }
    }

    [Fact]
    public void Begin_ShouldRestoreOuterValue_WhenNested()
    {
        ICorrelationContext context = NewContext("from-request");

        using (_scope.Begin("outer"))
        {
            using (_scope.Begin("inner"))
            {
                context.CorrelationId.ShouldBe("inner");
            }

            context.CorrelationId.ShouldBe("outer");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Begin_ShouldNotOverride_WhenIdIsNullOrBlank(string correlationId)
    {
        ICorrelationContext context = NewContext("from-request");

        using (_scope.Begin(correlationId))
        {
            context.CorrelationId.ShouldBe("from-request");
        }
    }

    [Fact]
    public async Task Begin_ShouldFlowAcrossAwaits_ButNotLeakToSiblingFlows()
    {
        ICorrelationContext context = NewContext("from-request");

        using (_scope.Begin("abc"))
        {
            await Task.Yield();
            context.CorrelationId.ShouldBe("abc");
        }

        var sibling = await Task.Run(() => context.CorrelationId);
        sibling.ShouldBe("from-request");
    }
}
