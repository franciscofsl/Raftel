using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raftel.Application.Abstractions;

namespace Raftel.Api.Server.Security;

internal static class RateLimitRejection
{
    public const string ErrorCode = "RateLimit.Exceeded";
    public const string RejectedField = "rate_limit.rejected";

    private static readonly TimeSpan FallbackRetryAfter = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Answers 429 with a <c>ProblemDetails</c> body and <c>Retry-After</c>, and raises the request's wide event to
    /// <see cref="LogLevel.Warning"/>: repeated rejections on credential endpoints signal a brute-force attempt.
    /// </summary>
    public static async ValueTask OnRejected(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var metadata)
            ? metadata
            : FallbackRetryAfter;

        httpContext.Response.Headers.RetryAfter =
            Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

        var requestEvent = httpContext.RequestServices.GetService<IRequestEvent>();
        requestEvent?.Set(RequestEventFields.Level, LogLevel.Warning);
        requestEvent?.Set(RejectedField, true);

        await Results.Problem(
                detail: "Too many requests. Retry after the time given in the Retry-After header.",
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too Many Requests",
                extensions: new Dictionary<string, object> { ["code"] = ErrorCode })
            .ExecuteAsync(httpContext);
    }
}
