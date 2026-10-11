using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Raftel.Application;
using Raftel.Application.Abstractions.Concurrency;
using Raftel.Application.Authorization;

namespace Raftel.Api.Server.AutoEndpoints;

/// <summary>
/// Reads the <c>If-Match</c> header of a command request and hands the version it carries to the
/// unit of work, which enforces it atomically when the command's changes are saved.
/// </summary>
internal static class IfMatchPrecondition
{
    /// <summary>
    /// Evaluates the header before the command is dispatched.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <param name="commandType">The type of the command about to be dispatched.</param>
    /// <returns>
    /// A rejection response when the request must not be dispatched; otherwise whether an expected
    /// version was set for this request.
    /// </returns>
    public static Outcome Evaluate(HttpContext context, Type commandType)
    {
        if (!context.RequestServices.GetRequiredService<ConcurrencyOptions>().EnableETags)
        {
            return Outcome.Proceed(expectedVersionSet: false);
        }

        var mustSendVersion = commandType.IsDefined(typeof(RequiresExpectedVersionAttribute), inherit: true);

        if (!context.Request.Headers.ContainsKey(HeaderNames.IfMatch))
        {
            return mustSendVersion ? Outcome.Reject(ErrorResults.PreconditionRequired()) : Outcome.Proceed(false);
        }

        var tags = context.Request.GetTypedHeaders().IfMatch;

        if (tags.Count != 1 || tags[0].IsWeak)
        {
            return Outcome.Reject(ErrorResults.MalformedIfMatch());
        }

        if (tags[0].Equals(EntityTagHeaderValue.Any))
        {
            return mustSendVersion ? Outcome.Reject(ErrorResults.PreconditionRequired()) : Outcome.Proceed(false);
        }

        context.RequestServices.GetRequiredService<IExpectedVersion>().Expect(tags[0].Tag.Value![1..^1]);
        return Outcome.Proceed(expectedVersionSet: true);
    }

    /// <summary>
    /// The result of evaluating the header.
    /// </summary>
    internal readonly record struct Outcome(IResult Rejection, bool ExpectedVersionSet)
    {
        public static Outcome Proceed(bool expectedVersionSet) => new(null, expectedVersionSet);

        public static Outcome Reject(IResult rejection) => new(rejection, false);
    }
}
