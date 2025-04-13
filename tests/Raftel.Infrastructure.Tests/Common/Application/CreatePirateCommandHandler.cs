using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Infrastructure.Tests.Common.PiratesEntities;
using Raftel.Infrastructure.Tests.Data.Common;

namespace Raftel.Infrastructure.Tests.Common.Application;

public sealed class CreatePirateCommandHandler(IPirateRepository repository) : ICommandHandler<CreatePirateCommand>
{
    public async Task<Result> HandleAsync(CreatePirateCommand request)
    {
        await repository.AddAsync(Pirate.Create(request.Name, request.Bounty));
        return Result.Success();
    }
}