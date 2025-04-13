using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.Middlewares;
using Raftel.Application.Commands;
using Raftel.Application.Queries;

namespace Raftel.Application;

public static class DependencyInjection
{
    public static void AddRaftelApplication(this IServiceCollection services)
    {
        services.AddScoped<IRequestDispatcher, RequestDispatcher>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        services.AddScoped(typeof(IRequestMiddleware<,>), typeof(UnitOfWorkMiddleware<,>));
        services.AddScoped(typeof(IRequestMiddleware<,>), typeof(ValidationMiddleware<,>));
    }
}