using System.Reflection;
using Raftel.Application;
using Raftel.Domain.BaseTypes;
using Shouldly;

namespace Raftel.ArchitectureTests;

public class CancellationTokenPropagationTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(RaftelApplicationBuilder).Assembly;

    [Fact]
    public void PublicAsyncMethods_In_DomainAndApplication_Should_DeclareCancellationToken()
    {
        var offenders = DomainAssembly.GetExportedTypes()
            .Concat(ApplicationAssembly.GetExportedTypes())
            .SelectMany(type => type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name.EndsWith("Async", StringComparison.Ordinal))
                .Where(method => method.GetParameters()
                    .All(parameter => parameter.ParameterType != typeof(CancellationToken)))
                .Select(method => $"{type.FullName}.{method.Name}"))
            .ToList();

        offenders.ShouldBeEmpty(
            $"Every public *Async method in Raftel.Domain and Raftel.Application must declare a CancellationToken parameter. Offenders: {string.Join(", ", offenders)}");
    }
}
