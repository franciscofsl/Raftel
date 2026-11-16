using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raftel.Application;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Users;
using Raftel.Domain.Features.Users.ValueObjects;

namespace Raftel.Infrastructure.Seeding.Seeders;

/// <summary>
/// Creates the initial administrator. There is never a default password: outside Development a
/// missing password or email fails the seeder, and in Development a random password is generated
/// and logged exactly once, when the user is created.
/// </summary>
internal sealed class AdminUserSeeder(
    IOptions<SeedOptions> options,
    IOptions<IdentityOptions> identityOptions,
    IHostEnvironment environment,
    IUsersRepository usersRepository,
    IRolesRepository rolesRepository,
    IAuthenticationService authenticationService,
    ILogger<AdminUserSeeder> logger) : IDataSeeder
{
    private const string DevelopmentEmail = "admin@raftel.local";
    private const string PasswordSetting = "Raftel:Seed:AdminPassword";
    private const string EmailSetting = "Raftel:Seed:AdminEmail";

    public int Order => 300;

    public string Name => "admin-user";

    public async Task<Result> SeedAsync(CancellationToken cancellationToken = default)
    {
        var seed = options.Value;
        var email = seed.AdminEmail;
        if (string.IsNullOrWhiteSpace(email))
        {
            if (!environment.IsDevelopment())
            {
                return Result.Failure(MissingSetting(EmailSetting));
            }

            email = DevelopmentEmail;
        }

        if (!Email.IsEmail(email))
        {
            return Result.Failure(Error.Validation("Seeding.InvalidAdminEmail", $"{EmailSetting} is not a valid email address."));
        }

        var role = await FindAdminRoleAsync(seed.AdminRoleName, cancellationToken);
        if (role.IsFailure)
        {
            return Result.Failure(role.Error);
        }

        var existing = await usersRepository.ListAllAsync(u => u.Email == email, cancellationToken);
        if (existing.Count > 0)
        {
            return await authenticationService.AssignRoleAsync(existing[0], role.Value, cancellationToken);
        }

        var password = ResolvePassword(seed.AdminPassword, email);
        if (password.IsFailure)
        {
            return Result.Failure(password.Error);
        }

        return await CreateAdministratorAsync(email, password.Value, role.Value, cancellationToken);
    }

    private async Task<Result> CreateAdministratorAsync(
        string email,
        string password,
        Role role,
        CancellationToken cancellationToken)
    {
        var user = User.Create(email, email, email);
        var registered = await authenticationService.RegisterAsync(user, password, cancellationToken);
        if (registered.IsFailure)
        {
            return Result.Failure(Error.Validation(
                "Seeding.AdminUserRejected",
                $"The administrator user could not be created: {registered.Error.Message}"));
        }

        user.BindTo(registered.Value);
        await usersRepository.AddAsync(user, cancellationToken);

        var assigned = await authenticationService.AssignRoleAsync(user, role, cancellationToken);
        if (assigned.IsFailure)
        {
            return assigned;
        }

        user.AssignRole(role);
        return Result.Success();
    }

    private async Task<Result<Role>> FindAdminRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var lowered = roleName.ToLower();
        var roles = await rolesRepository.ListAllAsync(r => r.Name.ToLower() == lowered, cancellationToken);
        return roles.Count > 0
            ? Result.Success(roles[0])
            : Result.Failure<Role>(Error.NotFound(
                "Seeding.AdminRoleNotFound",
                $"The administrator role '{roleName}' does not exist; make sure it is among the seeded roles."));
    }

    private Result<string> ResolvePassword(string? configured, string email)
    {
        if (!string.IsNullOrEmpty(configured))
        {
            return Result.Success(configured);
        }

        if (!environment.IsDevelopment())
        {
            return Result.Failure<string>(MissingSetting(PasswordSetting));
        }

        var generated = GeneratePassword(identityOptions.Value.Password);
        logger.LogWarning(
            "DEVELOPMENT ONLY: no {Setting} configured; generated a password for administrator {Email}: {GeneratedPassword}. " +
            "It is shown only once. Never rely on this outside Development.",
            PasswordSetting, email, generated);
        return Result.Success(generated);
    }

    private static Error MissingSetting(string setting) => Error.Validation(
        "Seeding.MissingAdminSetting",
        $"The initial administrator cannot be created: setting '{setting}' is missing. " +
        "Provide it through an environment variable, user-secrets or a key vault.");

    private static string GeneratePassword(PasswordOptions policy)
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string symbols = "!@#$%^&*-_=+?";

        var length = Math.Max(24, policy.RequiredLength);
        var characters = new List<char>
        {
            Pick(lower), Pick(upper), Pick(digits), Pick(symbols)
        };

        var all = lower + upper + digits + symbols;
        while (characters.Count < length)
        {
            characters.Add(Pick(all));
        }

        for (var i = characters.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (characters[i], characters[j]) = (characters[j], characters[i]);
        }

        return new string(characters.ToArray());
    }

    private static char Pick(string alphabet) => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
}
