using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.Auditing;

namespace Raftel.Application.Middlewares;

/// <summary>
/// Global middleware that opens an <see cref="IAuditLogScope"/> for the duration of the request,
/// identifying the command or query that is being processed so that infrastructure components
/// (e.g. the entity-change tracking interceptor) can attribute audited changes to it.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <param name="auditLogScope">The ambient audit log scope.</param>
public class AuditLogMiddleware<TRequest, TResponse>(IAuditLogScope auditLogScope) : IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Opens the audit log scope using the request's full type name, invokes the next delegate
    /// in the pipeline, and reverts the scope once the request has been handled.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The delegate representing the next middleware or handler in the pipeline.</param>
    /// <returns>The response returned by the next handler.</returns>
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        var commandName = request.GetType().FullName ?? request.GetType().Name;

        using (auditLogScope.Begin(commandName))
        {
            return await next();
        }
    }
}
