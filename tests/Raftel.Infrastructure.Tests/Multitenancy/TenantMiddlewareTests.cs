using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Raftel.Application.Features.Tenants;
using Raftel.Infrastructure.Authentication;
using Raftel.Infrastructure.Multitenancy;
using Raftel.Infrastructure.Multitenancy.Middleware;

namespace Raftel.Infrastructure.Tests.Multitenancy;

public class TenantMiddlewareTests
{
    private const string Header = "X-Tenant-Id";

    private readonly CurrentTenant _currentTenant = new();
    private readonly DefaultHttpContext _httpContext = new();
    private bool _nextInvoked;
    private Guid? _tenantSeenByNext;
    private readonly TenantMiddleware _middleware;

    public TenantMiddlewareTests()
    {
        _httpContext.Response.Body = new MemoryStream();
        _middleware = new TenantMiddleware(_ =>
        {
            _nextInvoked = true;
            _tenantSeenByNext = _currentTenant.Id;
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Anonymous_WithoutHeader_ShouldRunAsHost()
    {
        await InvokeAsync();

        ShouldReachNextWith(null);
    }

    [Fact]
    public async Task Anonymous_WithValidHeader_ShouldRunWithinTheRequestedTenant()
    {
        var tenantId = Guid.NewGuid();
        _httpContext.Request.Headers[Header] = tenantId.ToString();

        await InvokeAsync();

        ShouldReachNextWith(tenantId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Anonymous_WithBlankHeader_ShouldRunAsHost(string value)
    {
        _httpContext.Request.Headers[Header] = value;

        await InvokeAsync();

        ShouldReachNextWith(null);
    }

    [Theory]
    [InlineData("invalid-guid")]
    [InlineData("1234")]
    public async Task MalformedHeader_ShouldBeRejectedWith400WithoutReachingNext(string value)
    {
        _httpContext.Request.Headers[Header] = value;

        await InvokeAsync();

        await ShouldBeRejectedAsync(StatusCodes.Status400BadRequest, "Tenant.InvalidHeader");
    }

    [Fact]
    public async Task SeveralHeaderValues_ShouldBeRejectedWith400()
    {
        _httpContext.Request.Headers[Header] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

        await InvokeAsync();

        await ShouldBeRejectedAsync(StatusCodes.Status400BadRequest, "Tenant.InvalidHeader");
    }

    [Fact]
    public async Task TenantUser_WithoutHeader_ShouldRunWithinItsOwnTenant()
    {
        var tenantId = Guid.NewGuid();
        _httpContext.User = TenantUser(tenantId);

        await InvokeAsync();

        ShouldReachNextWith(tenantId);
    }

    [Fact]
    public async Task TenantUser_WithItsOwnTenantInTheHeader_ShouldRunWithinItsOwnTenant()
    {
        var tenantId = Guid.NewGuid();
        _httpContext.User = TenantUser(tenantId);
        _httpContext.Request.Headers[Header] = tenantId.ToString().ToUpperInvariant();

        await InvokeAsync();

        ShouldReachNextWith(tenantId);
    }

    [Fact]
    public async Task TenantUser_WithAnotherTenantInTheHeader_ShouldBeRejectedWith403()
    {
        _httpContext.User = TenantUser(Guid.NewGuid());
        _httpContext.Request.Headers[Header] = Guid.NewGuid().ToString();

        await InvokeAsync();

        await ShouldBeRejectedAsync(StatusCodes.Status403Forbidden, "Tenant.Forbidden");
    }

    [Fact]
    public async Task TenantUser_HoldingTheSwitchPermission_ShouldStillBePinnedToItsOwnTenant()
    {
        _httpContext.User = TenantUser(Guid.NewGuid(), TenantsPermissions.Switch);
        _httpContext.Request.Headers[Header] = Guid.NewGuid().ToString();

        await InvokeAsync();

        await ShouldBeRejectedAsync(StatusCodes.Status403Forbidden, "Tenant.Forbidden");
    }

    [Fact]
    public async Task UserWithAMalformedTenantClaim_ShouldBeRejectedWith403()
    {
        _httpContext.User = Authenticated(new Claim(RaftelClaimTypes.TenantId, "not-a-guid"));

        await InvokeAsync();

        await ShouldBeRejectedAsync(StatusCodes.Status403Forbidden, "Tenant.Forbidden");
    }

    [Fact]
    public async Task HostUser_WithoutHeader_ShouldRunAsHost()
    {
        _httpContext.User = HostUser();

        await InvokeAsync();

        ShouldReachNextWith(null);
    }

    [Fact]
    public async Task HostUser_WithHeaderButWithoutTheSwitchPermission_ShouldBeRejectedWith403()
    {
        _httpContext.User = HostUser(TenantsPermissions.View, TenantsPermissions.Management);
        _httpContext.Request.Headers[Header] = Guid.NewGuid().ToString();

        await InvokeAsync();

        await ShouldBeRejectedAsync(StatusCodes.Status403Forbidden, "Tenant.Forbidden");
    }

    [Fact]
    public async Task HostUser_WithHeaderAndTheSwitchPermission_ShouldRunWithinTheRequestedTenant()
    {
        var tenantId = Guid.NewGuid();
        _httpContext.User = HostUser(TenantsPermissions.Switch);
        _httpContext.Request.Headers[Header] = tenantId.ToString();

        await InvokeAsync();

        ShouldReachNextWith(tenantId);
    }

    [Fact]
    public async Task WhenRunningBeforeAuthentication_ShouldAuthenticateTheBearerTokenItselfAndPinItsTenant()
    {
        var tenantId = Guid.NewGuid();
        var principal = TenantUser(tenantId);
        UseAuthenticationResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Bearer")));
        _httpContext.Request.Headers.Authorization = "Bearer token";
        _httpContext.Request.Headers[Header] = Guid.NewGuid().ToString();

        await InvokeAsync();

        await ShouldBeRejectedAsync(StatusCodes.Status403Forbidden, "Tenant.Forbidden");
        _httpContext.User.ShouldBeSameAs(principal);
    }

    [Fact]
    public async Task WhenRunningBeforeAuthenticationWithoutHeader_ShouldUseTheTenantOfTheAuthenticatedToken()
    {
        var tenantId = Guid.NewGuid();
        UseAuthenticationResult(AuthenticateResult.Success(new AuthenticationTicket(TenantUser(tenantId), "Bearer")));
        _httpContext.Request.Headers.Authorization = "Bearer token";

        await InvokeAsync();

        ShouldReachNextWith(tenantId);
    }

    [Fact]
    public async Task WhenTheBearerTokenFailsAuthentication_ShouldTreatTheRequestAsAnonymous()
    {
        var tenantId = Guid.NewGuid();
        UseAuthenticationResult(AuthenticateResult.Fail("invalid token"));
        _httpContext.Request.Headers.Authorization = "Bearer invalid";
        _httpContext.Request.Headers[Header] = tenantId.ToString();

        await InvokeAsync();

        ShouldReachNextWith(tenantId);
    }

    [Fact]
    public async Task AfterTheRequest_ShouldRestoreThePreviousTenant()
    {
        var outer = Guid.NewGuid();
        _httpContext.User = TenantUser(Guid.NewGuid());

        using (_currentTenant.Change(outer))
        {
            await InvokeAsync();

            _currentTenant.Id.ShouldBe(outer);
        }
    }

    private Task InvokeAsync() => _middleware.InvokeAsync(_httpContext, _currentTenant);

    private void ShouldReachNextWith(Guid? tenantId)
    {
        _nextInvoked.ShouldBeTrue();
        _tenantSeenByNext.ShouldBe(tenantId);
        _httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    private async Task ShouldBeRejectedAsync(int statusCode, string code)
    {
        _nextInvoked.ShouldBeFalse();
        _httpContext.Response.StatusCode.ShouldBe(statusCode);
        _httpContext.Response.ContentType.ShouldStartWith("application/problem+json");

        _httpContext.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(_httpContext.Response.Body);
        body.RootElement.GetProperty("code").GetString().ShouldBe(code);
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(statusCode);
    }

    private void UseAuthenticationResult(AuthenticateResult result)
    {
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string?>()).Returns(result);
        _httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(authentication)
            .BuildServiceProvider();
    }

    private static ClaimsPrincipal TenantUser(Guid tenantId, params string[] permissions) =>
        Authenticated([new Claim(RaftelClaimTypes.TenantId, tenantId.ToString()), .. PermissionClaims(permissions)]);

    private static ClaimsPrincipal HostUser(params string[] permissions) => Authenticated(PermissionClaims(permissions));

    private static Claim[] PermissionClaims(string[] permissions) =>
        permissions.Select(p => new Claim(RaftelClaimTypes.Permission, p)).ToArray();

    private static ClaimsPrincipal Authenticated(params Claim[] claims) =>
        new(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), .. claims], "Bearer"));
}
