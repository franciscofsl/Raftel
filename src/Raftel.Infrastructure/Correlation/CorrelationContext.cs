using Microsoft.AspNetCore.Http;
using Raftel.Application.Abstractions;

namespace Raftel.Infrastructure.Correlation;

internal sealed class CorrelationContext(IHttpContextAccessor accessor) : ICorrelationContext
{
    private const string HeaderName = "X-Correlation-Id";

    private string? _correlationId;

    public string CorrelationId => _correlationId ??= Resolve();

    private string Resolve()
    {
        var headerValue = accessor.HttpContext?.Request.Headers[HeaderName].FirstOrDefault();

        return CorrelationIdSanitizer.IsValid(headerValue) ? headerValue! : Guid.NewGuid().ToString();
    }
}
