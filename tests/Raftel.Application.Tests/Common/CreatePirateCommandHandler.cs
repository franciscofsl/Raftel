using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Tests.Common.Domain;

namespace Raftel.Application.Tests.Common;

public sealed class CreatePirateCommandHandler(IPirateRepository repository) : ICommandHandler<CreatePirateCommand>
{
    public async Task<Result> HandleAsync(CreatePirateCommand request)
    {
        await repository.AddAsync(Pirate.Create(request.Name, request.Bounty));
        return Result.Success();
    }
}