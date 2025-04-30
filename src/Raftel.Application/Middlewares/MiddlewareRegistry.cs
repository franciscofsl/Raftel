namespace Raftel.Application.Abstractions;

internal record MiddlewareRegistry(
    List<Type> GlobalMiddlewares,
    List<Type> CommandMiddlewares,
    List<Type> QueryMiddlewares);