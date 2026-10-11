using Raftel.Application.Abstractions.Seeding;
using Raftel.Application.Authorization;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.UnitTests.Seeding;

public sealed class MinimalSeeder : IDataSeeder
{
    public int Order => 10;
    public string Name => "minimal";

    public Task<Result> SeedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());
}

public abstract class AbstractSeeder : IDataSeeder
{
    public int Order => 20;
    public abstract string Name { get; }

    public Task<Result> SeedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());
}

[PermissionSet]
public static class SamplePermissions
{
    public const string Read = "Sample.Read";
    public const string Write = "Sample.Write";
    public static readonly string NotAConstant = "Sample.NotAConstant";
    public const int NotAString = 3;
}

public static class UnmarkedPermissions
{
    public const string Hidden = "Unmarked.Hidden";
}
