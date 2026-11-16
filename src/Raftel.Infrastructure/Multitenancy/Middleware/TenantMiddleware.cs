using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Features.Tenants;
using Raftel.Infrastructure.Authentication;

namespace Raftel.Infrastructure.Multitenancy.Middleware;

internal sealed class TenantMiddleware(RequestDelegate next)
{
    internal const string TenantHeaderName = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant)
    {
        var requested = ReadRequestedTenant(context);
        if (requested.IsInvalid)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Tenant.InvalidHeader",
                $"The {TenantHeaderName} header must contain a single tenant identifier.");
            return;
        }

        var user = await ResolveUserAsync(context);
        var resolution = ResolveTenant(user, requested.TenantId);
        if (resolution.IsForbidden)
        {
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "Tenant.Forbidden",
                "The current user is not allowed to act within the requested tenant.");
            return;
        }

        using (currentTenant.Change(resolution.TenantId))
        {
            await next(context);
        }
    }

    private static TenantResolution ResolveTenant(ClaimsPrincipal? user, Guid? requestedTenantId)
    {
        if (user is null)
        {
            return TenantResolution.Allowed(requestedTenantId);
        }

        var tenantClaim = user.FindFirst(RaftelClaimTypes.TenantId)?.Value;
        if (tenantClaim is not null)
        {
            if (!Guid.TryParse(tenantClaim, out var userTenantId))
            {
                return TenantResolution.Forbidden;
            }

            return requestedTenantId is null || requestedTenantId == userTenantId
                ? TenantResolution.Allowed(userTenantId)
                : TenantResolution.Forbidden;
        }

        if (requestedTenantId is null)
        {
            return TenantResolution.Allowed(null);
        }

        return user.HasClaim(RaftelClaimTypes.Permission, TenantsPermissions.Switch)
            ? TenantResolution.Allowed(requestedTenantId)
            : TenantResolution.Forbidden;
    }

    private static async Task<ClaimsPrincipal?> ResolveUserAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return context.User;
        }

        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            return null;
        }

        var result = await context.AuthenticateAsync();
        if (!result.Succeeded)
        {
            return null;
        }

        context.User = result.Principal;
        return result.Principal;
    }

    private static RequestedTenant ReadRequestedTenant(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(TenantHeaderName, out var values))
        {
            return RequestedTenant.None;
        }

        var nonEmpty = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (nonEmpty.Length == 0)
        {
            return RequestedTenant.None;
        }

        if (nonEmpty.Length > 1 || !Guid.TryParse(nonEmpty[0], out var tenantId))
        {
            return RequestedTenant.Invalid;
        }

        return RequestedTenant.Of(tenantId);
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status403Forbidden ? "Forbidden" : "Bad Request",
            Detail = detail,
            Extensions = { ["code"] = code }
        };

        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }

    private readonly record struct RequestedTenant(Guid? TenantId, bool IsInvalid)
    {
        public static RequestedTenant None => new(null, false);
        public static RequestedTenant Invalid => new(null, true);
        public static RequestedTenant Of(Guid tenantId) => new(tenantId, false);
    }

    private readonly record struct TenantResolution(Guid? TenantId, bool IsForbidden)
    {
        public static TenantResolution Forbidden => new(null, true);
        public static TenantResolution Allowed(Guid? tenantId) => new(tenantId, false);
    }
}
