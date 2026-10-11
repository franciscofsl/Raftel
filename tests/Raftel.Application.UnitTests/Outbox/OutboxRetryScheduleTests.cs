using Raftel.Application.Abstractions.Outbox;
using Shouldly;

namespace Raftel.Application.UnitTests.Outbox;

public class OutboxRetryScheduleTests
{
    private static readonly TimeSpan Base = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Max = TimeSpan.FromMinutes(5);

    [Fact]
    public void DelayFor_ShouldBeBaseDelay_OnFirstAttempt()
    {
        OutboxRetrySchedule.DelayFor(1, Base, Max).ShouldBe(Base);
    }

    [Theory]
    [InlineData(2, 20)]
    [InlineData(3, 40)]
    [InlineData(4, 80)]
    [InlineData(5, 160)]
    public void DelayFor_ShouldDoubleWithEachAttempt(int attempt, int expectedSeconds)
    {
        OutboxRetrySchedule.DelayFor(attempt, Base, Max).ShouldBe(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public void DelayFor_ShouldNeverExceedMaxDelay()
    {
        OutboxRetrySchedule.DelayFor(6, Base, Max).ShouldBe(Max);
        OutboxRetrySchedule.DelayFor(20, Base, Max).ShouldBe(Max);
    }

    [Theory]
    [InlineData(64)]
    [InlineData(1_000)]
    [InlineData(int.MaxValue)]
    public void DelayFor_ShouldNotOverflow_ForLargeAttemptNumbers(int attempt)
    {
        OutboxRetrySchedule.DelayFor(attempt, Base, Max).ShouldBe(Max);
    }

    [Fact]
    public void DelayFor_ShouldTreatNonPositiveAttemptsAsFirst()
    {
        OutboxRetrySchedule.DelayFor(0, Base, Max).ShouldBe(Base);
    }
}
