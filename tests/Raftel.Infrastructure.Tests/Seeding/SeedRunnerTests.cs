using Raftel.Application;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;

namespace Raftel.Infrastructure.Tests.Seeding;

public class SeedRunnerTests
{
    private static readonly Error Boom = Error.Failure("Test.Boom", "boom");

    [Fact]
    public async Task RunAsync_ShouldExecuteInAscendingOrder()
    {
        var harness = new SeedRunnerHarness()
            .With(log => new ScriptedSeeder(log, "third", 300))
            .With(log => new ScriptedSeeder(log, "first", 100))
            .With(log => new ScriptedSeeder(log, "second", 200));

        var result = await harness.RunAsync();

        result.IsSuccess.ShouldBeTrue();
        harness.Log.Entries.Where(e => e.StartsWith("seed:")).ShouldBe(["seed:first", "seed:second", "seed:third"]);
    }

    [Fact]
    public async Task RunAsync_ShouldBreakTiesByName()
    {
        var harness = new SeedRunnerHarness()
            .With(log => new ScriptedSeeder(log, "b", 100))
            .With(log => new ScriptedSeeder(log, "a", 100));

        await harness.RunAsync();

        harness.Log.Entries.Where(e => e.StartsWith("seed:")).ShouldBe(["seed:a", "seed:b"]);
    }

    [Fact]
    public async Task RunAsync_ShouldFailBeforeAnySeederWhenNamesAreDuplicated()
    {
        var harness = new SeedRunnerHarness()
            .With(log => new ScriptedSeeder(log, "same", 100))
            .With(log => new ScriptedSeeder(log, "same", 200));

        var result = await harness.RunAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("same");
        harness.Log.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_ShouldCommitEachSeederInItsOwnTransaction()
    {
        var harness = new SeedRunnerHarness()
            .With(log => new ScriptedSeeder(log, "one", 100))
            .With(log => new ScriptedSeeder(log, "two", 200));

        await harness.RunAsync();

        harness.Log.Entries.ShouldBe([
            "tx.begin", "seed:one", "uow.commit", "tx.commit",
            "tx.begin", "seed:two", "uow.commit", "tx.commit"
        ]);
    }

    [Fact]
    public async Task RunAsync_ShouldRollBackAFailingSeederAndKeepEarlierOnes()
    {
        var harness = new SeedRunnerHarness()
            .With(log => new ScriptedSeeder(log, "ok", 100))
            .With(log => new ScriptedSeeder(log, "bad", 300, () => Task.FromResult(Result.Failure(Boom))));

        var result = await harness.RunAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("bad");
        harness.History.Applied.ShouldContainKey(("ok", null));
        harness.History.Applied.ShouldNotContainKey(("bad", null));
        harness.Log.Entries.ShouldContain("tx.rollback");
        harness.Log.Entries.Count(e => e == "tx.commit").ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_ShouldTreatAThrowingSeederAsFailed()
    {
        var harness = new SeedRunnerHarness()
            .With(log => new ScriptedSeeder(log, "throws", 100, () => throw new InvalidOperationException("kaput")));

        var result = await harness.RunAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("kaput");
        harness.Log.Entries.ShouldContain("tx.rollback");
        harness.History.Applied.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_WithFailFast_ShouldSkipLaterSeeders()
    {
        var harness = new SeedRunnerHarness(new SeedOptions { FailFastOnError = true })
            .With(log => new ScriptedSeeder(log, "bad", 200, () => Task.FromResult(Result.Failure(Boom))))
            .With(log => new ScriptedSeeder(log, "later", 300));

        await harness.RunAsync();

        harness.Log.Entries.ShouldNotContain("seed:later");
    }

    [Fact]
    public async Task RunAsync_WithoutFailFast_ShouldContinueAndStillReportFailure()
    {
        var harness = new SeedRunnerHarness(new SeedOptions { FailFastOnError = false })
            .With(log => new ScriptedSeeder(log, "bad", 200, () => Task.FromResult(Result.Failure(Boom))))
            .With(log => new ScriptedSeeder(log, "later", 300));

        var result = await harness.RunAsync();

        harness.Log.Entries.ShouldContain("seed:later");
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_ShouldSkipRunOnceSeederAlreadyApplied()
    {
        var harness = new SeedRunnerHarness()
            .With(log => new ScriptedSeeder(log, "once", 100, runOnce: true, version: 1));
        harness.History.Applied[("once", null)] = 1;

        await harness.RunAsync();

        harness.Log.Entries.ShouldNotContain("seed:once");
    }

    [Fact]
    public async Task RunAsync_ShouldReexecuteRunOnceSeederWhenVersionIsRaised()
    {
        var harness = new SeedRunnerHarness()
            .With(log => new ScriptedSeeder(log, "once", 100, runOnce: true, version: 2));
        harness.History.Applied[("once", null)] = 1;

        await harness.RunAsync();

        harness.Log.Entries.ShouldContain("seed:once");
        harness.History.Applied[("once", null)].ShouldBe(2);
    }

    [Fact]
    public async Task RunAsync_ShouldReexecuteReconcilingSeeders()
    {
        var harness = new SeedRunnerHarness()
            .With(log => new ScriptedSeeder(log, "reconcile", 100));
        harness.History.Applied[("reconcile", null)] = 1;

        await harness.RunAsync();

        harness.Log.Entries.ShouldContain("seed:reconcile");
    }

    [Fact]
    public async Task RunAsync_ShouldRunTenantSeedersOncePerTenantInThatTenantsContext()
    {
        var first = Tenant.Create("First", "FIRST", "d").Value;
        var second = Tenant.Create("Second", "SECOND", "d").Value;
        var harness = new SeedRunnerHarness(tenants: [first, second])
            .WithTenantSeeder("per-tenant", 100);

        var result = await harness.RunAsync();

        result.IsSuccess.ShouldBeTrue();
        var seeded = harness.Log.Entries.Where(e => e.StartsWith("seed:per-tenant")).ToList();
        seeded.Count.ShouldBe(2);
        seeded.ShouldContain(e => e.StartsWith($"seed:per-tenant:{((Guid)first.Id)}:current={((Guid)first.Id)}"));
        seeded.ShouldContain(e => e.StartsWith($"seed:per-tenant:{((Guid)second.Id)}:current={((Guid)second.Id)}"));
        harness.CurrentTenant.Id.ShouldBeNull();
        harness.History.Applied.ShouldContainKey(("per-tenant", (Guid?)((Guid)first.Id)));
        harness.History.Applied.ShouldContainKey(("per-tenant", (Guid?)((Guid)second.Id)));
    }

    [Fact]
    public async Task RunAsync_ShouldKeepFirstTenantWhenSecondTenantFails()
    {
        var first = Tenant.Create("First", "FIRST", "d").Value;
        var second = Tenant.Create("Second", "SECOND", "d").Value;
        var harness = new SeedRunnerHarness(new SeedOptions { FailFastOnError = false }, tenants: [first, second])
            .WithTenantSeeder("per-tenant", 100,
                tenantId => Task.FromResult(tenantId == second.Id ? Result.Failure(Boom) : Result.Success()));

        var result = await harness.RunAsync();

        result.IsFailure.ShouldBeTrue();
        harness.History.Applied.ShouldContainKey(("per-tenant", (Guid?)((Guid)first.Id)));
        harness.History.Applied.ShouldNotContainKey(("per-tenant", (Guid?)((Guid)second.Id)));
    }

    [Fact]
    public async Task RunForTenantAsync_ShouldRunOnlyTenantSeedersForThatTenant()
    {
        var tenant = Tenant.Create("First", "FIRST", "d").Value;
        var harness = new SeedRunnerHarness()
            .With(log => new ScriptedSeeder(log, "global", 100))
            .WithTenantSeeder("per-tenant", 200);

        var result = await harness.RunForTenantAsync(tenant.Id);

        result.IsSuccess.ShouldBeTrue();
        harness.Log.Entries.ShouldNotContain("seed:global");
        harness.Log.Entries.ShouldContain(e => e.StartsWith($"seed:per-tenant:{((Guid)tenant.Id)}:current={((Guid)tenant.Id)}"));
    }

    [Fact]
    public async Task RunForTenantAsync_ShouldRunInTheAmbientScope_SoItJoinsTheAmbientTransaction()
    {
        var tenant = Tenant.Create("First", "FIRST", "d").Value;
        var harness = new SeedRunnerHarness().WithTenantSeeder("per-tenant", 100);

        await harness.RunForTenantAsync(tenant.Id);

        harness.Log.Entries.ShouldContain(e => e.EndsWith($"scope={harness.AmbientScopeId}"));
    }

    [Fact]
    public async Task RunAsync_ShouldRunEachTenantExecutionInItsOwnScope()
    {
        var first = Tenant.Create("First", "FIRST", "d").Value;
        var second = Tenant.Create("Second", "SECOND", "d").Value;
        var harness = new SeedRunnerHarness(tenants: [first, second]).WithTenantSeeder("per-tenant", 100);

        await harness.RunAsync();

        var scopes = harness.Log.Entries.Where(e => e.StartsWith("seed:per-tenant")).Select(e => e[(e.IndexOf("scope=") + 6)..]).ToList();
        scopes.Distinct().Count().ShouldBe(2);
    }
}
