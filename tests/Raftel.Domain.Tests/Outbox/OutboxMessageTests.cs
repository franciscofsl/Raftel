using Raftel.Domain.Outbox;
using Shouldly;

namespace Raftel.Domain.Tests.Outbox;

public class OutboxMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static OutboxMessage NewMessage(Guid? tenantId = null, string? correlationId = null) =>
        OutboxMessage.Create("Demo.Event, Demo", "{}", Now.AddMinutes(-1), tenantId, correlationId);

    [Fact]
    public void Create_ShouldSetAllProperties_AndStartPending()
    {
        var tenantId = Guid.NewGuid();

        var message = OutboxMessage.Create("Demo.Event, Demo", "{\"a\":1}", Now, tenantId, "corr-1");

        message.Id.ShouldNotBeNull();
        message.Type.ShouldBe("Demo.Event, Demo");
        message.Content.ShouldBe("{\"a\":1}");
        message.OccurredOn.ShouldBe(Now);
        message.TenantId.ShouldBe(tenantId);
        message.CorrelationId.ShouldBe("corr-1");
        message.ProcessedOn.ShouldBeNull();
        message.AttemptCount.ShouldBe(0);
        message.Error.ShouldBeNull();
        message.NextAttemptOn.ShouldBeNull();
        message.LockedUntil.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldAllowNullTenantAndCorrelation()
    {
        var message = NewMessage();

        message.TenantId.ShouldBeNull();
        message.CorrelationId.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_ShouldRejectBlankType(string type)
    {
        Should.Throw<ArgumentException>(() => OutboxMessage.Create(type, "{}", Now, null, null));
    }

    [Fact]
    public void Claim_ShouldLeaseAndCountAttempt()
    {
        var message = NewMessage();

        message.Claim(Now.AddMinutes(1));

        message.LockedUntil.ShouldBe(Now.AddMinutes(1));
        message.AttemptCount.ShouldBe(1);
    }

    [Fact]
    public void MarkProcessed_ShouldSetProcessedOn_AndClearErrorAndLease()
    {
        var message = NewMessage();
        message.Claim(Now.AddMinutes(1));
        message.MarkFailed(Now, "boom", Now.AddSeconds(10));
        message.Claim(Now.AddMinutes(1));

        message.MarkProcessed(Now.AddSeconds(30));

        message.ProcessedOn.ShouldBe(Now.AddSeconds(30));
        message.Error.ShouldBeNull();
        message.LockedUntil.ShouldBeNull();
        message.NextAttemptOn.ShouldBeNull();
    }

    [Fact]
    public void MarkFailed_ShouldStoreError_AndNextAttempt_AndClearLease_WithoutTouchingAttempts()
    {
        var message = NewMessage();
        message.Claim(Now.AddMinutes(1));

        message.MarkFailed(Now, "boom", Now.AddSeconds(10));

        message.Error.ShouldBe("boom");
        message.NextAttemptOn.ShouldBe(Now.AddSeconds(10));
        message.LockedUntil.ShouldBeNull();
        message.AttemptCount.ShouldBe(1);
        message.ProcessedOn.ShouldBeNull();
    }

    [Fact]
    public void MarkFailed_ShouldTruncateLongErrors()
    {
        var message = NewMessage();

        message.MarkFailed(Now, new string('x', 10_000), Now.AddSeconds(10));

        message.Error!.Length.ShouldBe(OutboxMessage.MaxErrorLength);
    }

    [Fact]
    public void Release_ShouldClearLease_AndHandBackOneAttempt()
    {
        var message = NewMessage();
        message.Claim(Now.AddMinutes(1));

        message.Release();

        message.LockedUntil.ShouldBeNull();
        message.AttemptCount.ShouldBe(0);
    }

    [Fact]
    public void Release_ShouldNeverDropAttemptsBelowZero()
    {
        var message = NewMessage();

        message.Release();

        message.AttemptCount.ShouldBe(0);
    }

    [Fact]
    public void IsDeadLetter_ShouldBeTrueOnlyWhenUnprocessedAndAtOrOverLimit()
    {
        var message = NewMessage();
        message.Claim(Now);
        message.Claim(Now);

        message.IsDeadLetter(3).ShouldBeFalse();

        message.Claim(Now);
        message.IsDeadLetter(3).ShouldBeTrue();

        message.Claim(Now);
        message.IsDeadLetter(3).ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLetter_ShouldBeFalseOnceProcessed()
    {
        var message = NewMessage();
        message.Claim(Now);
        message.Claim(Now);
        message.MarkProcessed(Now);

        message.IsDeadLetter(2).ShouldBeFalse();
    }
}
