# Getting Started

This guide takes a Raftel API from an empty database to a usable system: permissions in the catalog, an
administrator role, an administrator user and, optionally, a default tenant.

## Startup flow

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRaftelApplication(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly); // framework features
    cfg.RegisterServicesFromAssembly(typeof(MyFeatureCommand).Assembly);    // your handlers, seeders and permissions
});
builder.Services.AddRaftelData<MyDbContext>(builder.Configuration);
builder.Services.AddRaftelHealthChecks();

var app = builder.Build();

app.MapRaftelHealthChecks();

await app.MigrateAndSeedAsync();   // migrations (optional) + seeders; call it before app.Run()
app.Run();
```

`MigrateAndSeedAsync` does, in order:

1. Applies pending migrations, **only** if `Raftel:Seed:ApplyMigrationsOnStartup` is `true`.
   Otherwise, if migrations are pending, it fails with a message saying so and runs no seeder.
2. Runs every seeder (unless `RunSeedersOnStartup` is `false`).
3. Reports progress to `/health/ready`: not ready while it runs, ready when it completes.

If seeding fails and `FailFastOnError` is `true` (the default) it throws `SeedingFailedException`, which
aborts startup.

## Migrations

Seeding adds two tables (`PermissionDefinition`, `SeedHistory`). Add an EF Core migration for them to your
own project:

```bash
dotnet ef migrations add Add_Seeding --project <your infrastructure project>
```

The same migration also carries a mapping fix: role permissions used to live in a private collection EF never
mapped, so they were silently lost. They are now stored in the `Permission` table (foreign key `RoleId`), the
always-empty `PermissionRole` join table is dropped and `Permission.IsDeleted` goes away. Nothing could write
those rows before, so the migration loses no data.

**Do not apply migrations from the application in a multi-replica deployment.** Replicas starting together
race. Run migrations in a separate job and keep `ApplyMigrationsOnStartup = false` (the default); the option
exists for development and single-instance deployments. For the same reason, prefer one seeding job to every
replica seeding at startup; unique indexes make a race fail instead of duplicating data.

## Configuration (`Raftel:Seed`)

| Setting | Default | Meaning |
|---|---|---|
| `ApplyMigrationsOnStartup` | `false` | apply pending migrations before seeding |
| `RunSeedersOnStartup` | `true` | run the seeders |
| `FailFastOnError` | `true` | stop at the first failing seeder and abort startup |
| `AdminEmail` | none | email of the initial administrator; required outside Development |
| `AdminPassword` | none | password of the initial administrator; see below |
| `AdminRoleName` | `Admin` | name of the administrator role |
| `Roles` | one `Admin` role | roles to seed: `Name`, `Description`, `Permissions` (`*` = every active permission) |
| `DefaultTenant` | none | `Name`, `Code`, `Description` of a tenant to create; unset means no tenant is seeded |

```json
{
  "Raftel": {
    "Seed": {
      "AdminEmail": "admin@example.com",
      "DefaultTenant": { "Name": "Default", "Code": "DEFAULT", "Description": "Default tenant" }
    }
  }
}
```

### The administrator password

Raftel never ships a default password and never reads one from a committed file.

- Provide it through an environment variable (`Raftel__Seed__AdminPassword`), user-secrets or a key vault.
- Outside Development, a missing password (or email) makes the administrator seeder fail with a message
  naming the missing setting; with fail-fast on, the application does not start. Nothing is created.
- In Development only, a missing password is replaced by a random one that is written to the log **once**,
  when the user is created, marked as development-only. Later starts find the user and do not touch it.
- A configured password is never logged. If Identity rejects it, the failure message states the policy rule.
- The administrator is created without `MustChangePassword`: neither `User` nor the Identity setup supports
  it yet.

## What the built-in seeders do

| Seeder | Order | Behaviour |
|---|---|---|
| `default-tenant` | 50 | creates the configured default tenant if no tenant has its code |
| `permissions` | 100 | reconciles the permission catalog with the `const string` fields of every class marked `[PermissionSet]` |
| `default-roles` | 200 | creates the configured roles (and their Identity roles) and grants any missing permission; never removes one |
| `admin-user` | 300 | creates the administrator and assigns the administrator role; leaves an existing user alone |

All of them are reconcilers: running them again changes nothing, and a permission added to your code reaches
the catalog and the administrator role on the next run. A permission removed from code is flagged obsolete
and logged; it is never deleted, because roles may still hold it.

Mark your own permission classes so they are picked up:

```csharp
[PermissionSet]
public static class PiratesPermissions
{
    public const string View = "Pirates.View";
    public const string Management = "Pirates.Management";
}
```

## Writing a seeder

Implement `IDataSeeder` anywhere in an assembly registered with `AddRaftelApplication`; it is discovered
automatically. The demo's `OpenIddictClientSeeder` is a working example.

```csharp
public sealed class SupportRoleSeeder(IRolesRepository roles) : IDataSeeder
{
    public int Order => 500;          // lower runs first; ties are broken by Name
    public string Name => "support-role";

    public async Task<Result> SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await roles.ListAllAsync(r => r.Name == "Support", cancellationToken);
        if (existing.Count > 0)
        {
            return Result.Success();   // idempotent: running twice changes nothing
        }

        var role = Role.Create("Support", "Read-only support staff");
        if (role.IsFailure)
        {
            return Result.Failure(role.Error);
        }

        await roles.AddAsync(role.Value, cancellationToken);
        return Result.Success();
    }
}
```

Rules every seeder must respect:

- **Idempotent.** Check before inserting; the runner calls reconcilers on every start.
- **Own transaction.** Each seeder runs in its own scope and transaction. The runner commits when you return
  success and rolls back when you return a failure or throw. A failing seeder never reverts earlier ones.
- **Return `Result`.** Report business failures as a failed `Result`, not an exception.
- `Version` and `RunOnce` (defaulted interface members) let a seeder run only once per version: set
  `RunOnce => true` and raise `Version` when the data must be re-applied.

### Tenant seeders

Implement `ITenantDataSeeder` to seed data that belongs to a tenant. The runner executes it once per existing
tenant at startup, with the current tenant set, and again whenever a tenant is created through the
application (the `TenantCreated` domain event). If the command runs inside `TransactionMiddleware`, a failing
tenant seeder rolls back the tenant creation too.

```csharp
public sealed class TenantManagerRoleSeeder(IRolesRepository roles) : ITenantDataSeeder
{
    public int Order => 10;
    public string Name => "tenant-manager-role";

    public async Task<Result> SeedForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        // ICurrentTenant is already set to tenantId, so new rows get the right TenantId.
        var role = Role.Create("Tenant manager");
        if (role.IsFailure)
        {
            return Result.Failure(role.Error);
        }

        await roles.AddAsync(role.Value, cancellationToken);
        return Result.Success();
    }
}
```

## Known limitation: roles are seeded in host context

The default roles and the administrator are created without a tenant. When a request selects a tenant
(`X-Tenant-Id`), the tenant query filter hides rows that have no tenant, so a host-level role is not visible
inside a tenant context. Per-tenant default roles are not provided by the framework; use an
`ITenantDataSeeder` if you need them.

## Health

`/health/ready` includes a `seeding` check, so an orchestrator does not route traffic to an instance that is
still seeding. See [Health Checks](health-checks.md).
