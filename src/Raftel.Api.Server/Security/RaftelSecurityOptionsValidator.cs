using System.Net;
using Microsoft.Extensions.Options;

namespace Raftel.Api.Server.Security;

internal sealed class RaftelSecurityOptionsValidator : IValidateOptions<RaftelSecurityOptions>
{
    public ValidateOptionsResult Validate(string name, RaftelSecurityOptions options)
    {
        var failures = new List<string>();

        ValidateCors(options.Cors, failures);
        ValidatePolicy(nameof(RaftelRateLimitOptions.Global), options.RateLimiting.Global, failures);
        ValidatePolicy(nameof(RaftelRateLimitOptions.Auth), options.RateLimiting.Auth, failures);
        ValidatePolicy(nameof(RaftelRateLimitOptions.Write), options.RateLimiting.Write, failures);
        ValidateForwardedHeaders(options.ForwardedHeaders, failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateCors(RaftelCorsOptions cors, List<string> failures)
    {
        if (cors.AllowCredentials && cors.AllowsAnyOrigin)
        {
            failures.Add(
                $"{RaftelSecurityOptions.SectionName}:Cors: AllowCredentials cannot be combined with the '*' origin. " +
                "List the allowed origins explicitly.");
        }
    }

    private static void ValidatePolicy(string policy, RateLimitPolicyOptions options, List<string> failures)
    {
        if (options.PermitLimit <= 0)
        {
            failures.Add($"{RaftelSecurityOptions.SectionName}:RateLimiting:{policy}: PermitLimit must be greater than zero.");
        }

        if (options.Window <= TimeSpan.Zero)
        {
            failures.Add($"{RaftelSecurityOptions.SectionName}:RateLimiting:{policy}: Window must be greater than zero.");
        }
    }

    private static void ValidateForwardedHeaders(RaftelForwardedHeadersOptions forwarded, List<string> failures)
    {
        foreach (var proxy in forwarded.KnownProxies.Where(proxy => !IPAddress.TryParse(proxy, out _)))
        {
            failures.Add($"{RaftelSecurityOptions.SectionName}:ForwardedHeaders: '{proxy}' is not a valid IP address.");
        }

        foreach (var network in forwarded.KnownNetworks.Where(network => !IPNetwork.TryParse(network, out _)))
        {
            failures.Add($"{RaftelSecurityOptions.SectionName}:ForwardedHeaders: '{network}' is not a valid CIDR network.");
        }

        if (forwarded.ForwardLimit <= 0)
        {
            failures.Add($"{RaftelSecurityOptions.SectionName}:ForwardedHeaders: ForwardLimit must be greater than zero.");
        }
    }
}
