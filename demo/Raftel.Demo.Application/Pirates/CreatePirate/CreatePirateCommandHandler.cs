using Raftel.Application.Commands;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Application.Pirates.CreatePirate;

public sealed class CreatePirateCommandHandler(IPirateRepository repository) : ICommandHandler<CreatePirateCommand>
{
    public async Task<Result> HandleAsync(CreatePirateCommand request, CancellationToken token = default)
    {
        await repository.AddAsync(Pirate.Normal(request.Name, request.Bounty));
        return Result.Success();
    }
}