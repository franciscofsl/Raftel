namespace Raftel.Infrastructure.Outbox;

/// <summary>The result of delivering one outbox message to its handlers.</summary>
internal sealed record DeliveryOutcome(bool Succeeded, string Error, Exception Exception)
{
    public static readonly DeliveryOutcome Success = new(true, null, null);

    public static DeliveryOutcome Failed(string error, Exception exception = null) => new(false, error, exception);
}
