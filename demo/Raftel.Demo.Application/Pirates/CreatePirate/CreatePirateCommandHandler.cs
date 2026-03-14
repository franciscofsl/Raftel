using Raftel.Application.Commands;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Application.Pirates.CreatePirate;

public sealed class CreatePirateCommandHandler(IPirateRepository repository)
    : ICommandHandler<CreatePirateCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreatePirateCommand request, CancellationToken token = default)
    {
        var pirate = Pirate.Normal(request.Name, request.Bounty);
        await repository.AddAsync(pirate, token);
        return Result.Success((Guid)pirate.Id);
    }
}