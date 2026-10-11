using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raftel.Application;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Infrastructure.Seeding;

internal sealed class SeedRunner(
    IServiceProvider ambientServices,
    IServiceScopeFactory scopeFactory,
    IOptions<SeedOptions> options,
    ILogger<SeedRunner> logger) : ISeedRunner
{
    private sealed record PlannedSeeder(string Name, int Order, int Version, bool RunOnce, bool IsTenantSeeder);

    public async Task<Result> RunAsync(CancellationToken cancellationToken = default)
    {
        var plan = PlanFrom(DescribeSeeders());
        if (plan.IsFailure)
        {
            return Result.Failure(plan.Error);
        }

        var failures = new List<Error>();
        var failFast = options.Value.FailFastOnError;

        foreach (var seeder in plan.Value.Where(s => !s.IsTenantSeeder))
        {
            var result = await ExecuteInOwnScopeAsync(seeder, tenantId: null, cancellationToken);
            if (result.IsSuccess)
            {
                continue;
            }

            failures.Add(result.Error);
            if (failFast)
            {
                return Result.Failure(SeedErrors.SeedingFailed(failures));
            }
        }

        var tenantSeeders = plan.Value.Where(s => s.IsTenantSeeder).ToList();
        if (tenantSeeders.Count > 0)
        {
            var tenantIds = await ListTenantIdsAsync(cancellationToken);
            foreach (var tenantId in tenantIds)
            {
                foreach (var seeder in tenantSeeders)
                {
                    var result = await ExecuteInOwnScopeAsync(seeder, tenantId, cancellationToken);
                    if (result.IsSuccess)
                    {
                        continue;
                    }

                    failures.Add(result.Error);
                    if (failFast)
                    {
                        return Result.Failure(SeedErrors.SeedingFailed(failures));
                    }
                }
            }
        }

        return failures.Count == 0 ? Result.Success() : Result.Failure(SeedErrors.SeedingFailed(failures));
    }

    public async Task<Result> RunForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        var plan = PlanFrom(Describe(ambientServices));
        if (plan.IsFailure)
        {
            return Result.Failure(plan.Error);
        }

        var failures = new List<Error>();
        foreach (var seeder in plan.Value.Where(s => s.IsTenantSeeder))
        {
            var result = await ExecuteAsync(ambientServices, seeder, tenantId, cancellationToken);
            if (result.IsFailure)
            {
                failures.Add(result.Error);
                break;
            }
        }

        return failures.Count == 0 ? Result.Success() : Result.Failure(SeedErrors.SeedingFailed(failures));
    }

    private IReadOnlyList<PlannedSeeder> DescribeSeeders()
    {
        using var scope = scopeFactory.CreateScope();
        return Describe(scope.ServiceProvider);
    }

    private static IReadOnlyList<PlannedSeeder> Describe(IServiceProvider services)
    {
        return services.GetServices<IDataSeeder>()
            .Select(s => new PlannedSeeder(s.Name, s.Order, s.Version, s.RunOnce, s is ITenantDataSeeder))
            .ToList();
    }

    private static Result<IReadOnlyList<PlannedSeeder>> PlanFrom(IReadOnlyList<PlannedSeeder> seeders)
    {
        var duplicate = seeders.GroupBy(s => s.Name, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            return Result.Failure<IReadOnlyList<PlannedSeeder>>(SeedErrors.DuplicateSeederName(duplicate.Key));
        }

        IReadOnlyList<PlannedSeeder> ordered = seeders
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .ToList();
        return Result.Success(ordered);
    }

    private async Task<IReadOnlyList<TenantId>> ListTenantIdsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var tenants = await scope.ServiceProvider.GetRequiredService<ITenantsRepository>()
            .ListAllAsync(cancellationToken: cancellationToken);
        return tenants.Select(t => t.Id).ToList();
    }

    private async Task<Result> ExecuteInOwnScopeAsync(
        PlannedSeeder planned,
        TenantId? tenantId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        return await ExecuteAsync(scope.ServiceProvider, planned, tenantId, cancellationToken);
    }

    private async Task<Result> ExecuteAsync(
        IServiceProvider services,
        PlannedSeeder planned,
        TenantId? tenantId,
        CancellationToken cancellationToken)
    {
        Guid? tenantGuid = tenantId is null ? null : (Guid)tenantId;
        var history = services.GetRequiredService<ISeedHistoryStore>();

        if (planned.RunOnce
            && await history.GetAppliedVersionAsync(planned.Name, tenantGuid, cancellationToken) is { } applied
            && applied >= planned.Version)
        {
            logger.LogInformation("Seeder {Seeder} already applied at version {Version}; skipped.", planned.Name, applied);
            return Result.Success();
        }

        var seeder = services.GetServices<IDataSeeder>().First(s => s.Name == planned.Name);
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var currentTenant = services.GetRequiredService<ICurrentTenant>();

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        Result result;
        try
        {
            using (tenantGuid is null ? null : currentTenant.Change(tenantGuid))
            {
                result = tenantId is null
                    ? await seeder.SeedAsync(cancellationToken)
                    : await ((ITenantDataSeeder)seeder).SeedForTenantAsync(tenantId, cancellationToken);

                if (result.IsSuccess)
                {
                    await history.RecordAsync(planned.Name, tenantGuid, planned.Version, cancellationToken);
                    await unitOfWork.CommitAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    logger.LogInformation("Seeder {Seeder} applied.", planned.Name);
                    return result;
                }
            }

            result = Result.Failure(SeedErrors.SeederFailed(planned.Name, result.Error));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Seeder {Seeder} threw an exception.", planned.Name);
            var root = exception.GetBaseException();
            var detail = ReferenceEquals(root, exception) ? exception.Message : $"{exception.Message} {root.Message}";
            result = Result.Failure(SeedErrors.SeederThrew(planned.Name, detail));
        }

        await transaction.RollbackAsync(CancellationToken.None);
        logger.LogError("Seeder {Seeder} failed and was rolled back: {Error}", planned.Name, result.Error.Message);
        return result;
    }
}
