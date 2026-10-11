using Shouldly;

namespace Raftel.Application.UnitTests.Outbox;

public class OutboxOptionsTests
{
    [Fact]
    public void Defaults_ShouldMatchDocumentedValues()
    {
        var options = new OutboxOptions();

        options.Enabled.ShouldBeFalse();
        options.BatchSize.ShouldBe(50);
        options.PollingInterval.ShouldBe(TimeSpan.FromSeconds(5));
        options.MaxAttempts.ShouldBe(5);
        options.BaseRetryDelay.ShouldBe(TimeSpan.FromSeconds(10));
        options.MaxRetryDelay.ShouldBe(TimeSpan.FromMinutes(5));
        options.LockDuration.ShouldBe(TimeSpan.FromMinutes(1));
        options.RetentionPeriod.ShouldBe(TimeSpan.FromDays(7));
    }

    [Fact]
    public void Validate_ShouldPass_WithDefaults()
    {
        Should.NotThrow(() => new OutboxOptions { Enabled = true }.Validate());
    }

    [Theory]
    [InlineData(nameof(OutboxOptions.BatchSize))]
    [InlineData(nameof(OutboxOptions.MaxAttempts))]
    [InlineData(nameof(OutboxOptions.PollingInterval))]
    [InlineData(nameof(OutboxOptions.BaseRetryDelay))]
    [InlineData(nameof(OutboxOptions.MaxRetryDelay))]
    [InlineData(nameof(OutboxOptions.LockDuration))]
    [InlineData(nameof(OutboxOptions.RetentionPeriod))]
    public void Validate_ShouldThrow_NamingTheSetting_WhenNotPositive(string setting)
    {
        var options = new OutboxOptions { Enabled = true };
        Zero(options, setting);

        var exception = Should.Throw<InvalidOperationException>(options.Validate);

        exception.Message.ShouldContain(setting);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxRetryDelayIsBelowBaseRetryDelay()
    {
        var options = new OutboxOptions
        {
            Enabled = true,
            BaseRetryDelay = TimeSpan.FromMinutes(1),
            MaxRetryDelay = TimeSpan.FromSeconds(10)
        };

        var exception = Should.Throw<InvalidOperationException>(options.Validate);

        exception.Message.ShouldContain(nameof(OutboxOptions.MaxRetryDelay));
    }

    private static void Zero(OutboxOptions options, string setting)
    {
        switch (setting)
        {
            case nameof(OutboxOptions.BatchSize): options.BatchSize = 0; break;
            case nameof(OutboxOptions.MaxAttempts): options.MaxAttempts = -1; break;
            case nameof(OutboxOptions.PollingInterval): options.PollingInterval = TimeSpan.Zero; break;
            case nameof(OutboxOptions.BaseRetryDelay): options.BaseRetryDelay = TimeSpan.Zero; break;
            case nameof(OutboxOptions.MaxRetryDelay): options.MaxRetryDelay = TimeSpan.Zero; break;
            case nameof(OutboxOptions.LockDuration): options.LockDuration = TimeSpan.FromSeconds(-1); break;
            case nameof(OutboxOptions.RetentionPeriod): options.RetentionPeriod = TimeSpan.Zero; break;
        }
    }
}
