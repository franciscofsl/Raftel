namespace Raftel.Application.Middlewares;

internal record MiddlewareRegistry(
    List<Type> GlobalMiddlewares,
    List<Type> CommandMiddlewares,
    List<Type> QueryMiddlewares);