using Microsoft.AspNetCore.Builder;

namespace Raftel.Api.Server.Middlewares;

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseRaftelExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
