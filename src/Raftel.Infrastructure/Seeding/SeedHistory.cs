namespace Raftel.Infrastructure.Seeding;

/// <summary>
/// Records the last successful execution of a seeder, per tenant for tenant seeders.
/// </summary>
public sealed class SeedHistory
{
    private SeedHistory()
    {
    }

    public SeedHistory(string name, Guid? tenantId, int version, DateTime appliedOn)
    {
        Id = Guid.NewGuid();
        Name = name;
        TenantId = tenantId;
        Version = version;
        AppliedOn = appliedOn;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? TenantId { get; private set; }
    public int Version { get; private set; }
    public DateTime AppliedOn { get; private set; }

    public void Reapply(int version, DateTime appliedOn)
    {
        Version = version;
        AppliedOn = appliedOn;
    }
}
