using Raftel.Domain.Abstractions;

namespace Raftel.Infrastructure.Seeding;

public static class SeedErrors
{
    public static Error DuplicateSeederName(string name) =>
        Error.Conflict("Seeding.DuplicateSeederName", $"More than one seeder is named '{name}'. Seeder names must be unique.");

    public static Error SeederFailed(string name, Error error) =>
        new("Seeding.SeederFailed", $"Seeder '{name}' failed: {error.Message}", error.Type);

    public static Error SeederThrew(string name, string message) =>
        Error.Failure("Seeding.SeederThrew", $"Seeder '{name}' threw an exception: {message}");

    public static Error MigrationsPending(IReadOnlyCollection<string> pending) =>
        Error.Failure("Seeding.MigrationsPending",
            $"Database migrations are pending ({string.Join(", ", pending)}); no seeder was executed. " +
            "Apply the migrations first, or enable Raftel:Seed:ApplyMigrationsOnStartup for single-instance deployments.");

    public static Error SeedingFailed(IReadOnlyCollection<Error> errors) =>
        Error.Failure("Seeding.Failed", string.Join(" ", errors.Select(error => error.Message)));
}
