using Microsoft.Extensions.DependencyInjection;

namespace Raftel.Application.Commands;

public class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public async Task DispatchAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        await handler.HandleAsync(command);
    }
}