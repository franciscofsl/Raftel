namespace Raftel.Application.Abstractions.Health;

public sealed class HealthOptions
{
    public bool RequireAuthorizationForDetails { get; set; } = true;
    public string DetailsPolicy { get; set; }
    public TimeSpan CheckTimeout { get; set; } = TimeSpan.FromSeconds(3);
    public TimeSpan DatabaseSlowThreshold { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan OutboxLagThreshold { get; set; } = TimeSpan.FromMinutes(5);
    public int DeadLetterThreshold { get; set; } = 10;
    public bool EnableTenantResolutionCheck { get; set; }
    public bool EnableOutboxCheck { get; set; }
}
