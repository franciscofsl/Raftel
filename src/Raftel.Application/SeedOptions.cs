namespace Raftel.Application;

/// <summary>
/// Configures startup seeding. Bound from the <c>Raftel:Seed</c> configuration section.
/// </summary>
public sealed class SeedOptions
{
    /// <summary>The configuration section the options are bound from.</summary>
    public const string SectionName = "Raftel:Seed";

    /// <summary>
    /// Whether pending migrations are applied before seeding. Defaults to <see langword="false"/>:
    /// applying migrations from several replicas at once races; prefer a separate migration job.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    /// <summary>
    /// Whether seeders run on startup.
    /// </summary>
    public bool RunSeedersOnStartup { get; set; } = true;

    /// <summary>
    /// Whether the first failing seeder stops the remaining ones and aborts startup.
    /// </summary>
    public bool FailFastOnError { get; set; } = true;

    /// <summary>
    /// Email of the initial administrator. Required outside Development.
    /// </summary>
    public string? AdminEmail { get; set; }

    /// <summary>
    /// Password of the initial administrator. Never defaulted: supply it through an environment
    /// variable, user-secrets or a vault, never a committed file.
    /// </summary>
    public string? AdminPassword { get; set; }

    /// <summary>
    /// Name of the administrator role.
    /// </summary>
    public string AdminRoleName { get; set; } = "Admin";

    /// <summary>
    /// Roles to seed. When empty, a single <see cref="AdminRoleName"/> role holding every
    /// non-obsolete catalog permission is seeded.
    /// </summary>
    public List<SeedRoleOptions> Roles { get; set; } = new();

    /// <summary>
    /// The default tenant. When <see langword="null"/>, no tenant is seeded.
    /// </summary>
    public SeedTenantOptions? DefaultTenant { get; set; }
}

/// <summary>
/// A role to seed with its permissions. The permission <c>*</c> stands for every non-obsolete
/// catalog permission.
/// </summary>
public sealed class SeedRoleOptions
{
    /// <summary>Wildcard standing for every non-obsolete catalog permission.</summary>
    public const string AllPermissions = "*";

    /// <summary>Gets or sets the role name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the role description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the permission names granted to the role.</summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// The default tenant to seed.
/// </summary>
public sealed class SeedTenantOptions
{
    /// <summary>Gets or sets the tenant name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique tenant code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant description.</summary>
    public string Description { get; set; } = string.Empty;
}
