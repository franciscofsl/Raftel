namespace Raftel.Api.FunctionalTests.DemoApi.TestSupport;

/// <summary>Response of the test-only endpoint that reports the tenant resolved for the request.</summary>
public sealed record CurrentTenantProbe(Guid? TenantId);
