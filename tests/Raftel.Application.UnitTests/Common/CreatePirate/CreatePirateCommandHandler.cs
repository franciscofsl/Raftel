using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Tests.Common.Domain;

namespace Raftel.Application.UnitTests.Common.CreatePirate;

public sealed class CreatePirateCommandHandler(IPirateRepository repository) : ICommandHandler<CreatePirateCommand>
{
    public async Task<Result> HandleAsync(CreatePirateCommand request, CancellationToken token = default)
    {
        await repository.AddAsync(Pirate.Create(request.Name, request.Bounty));
        return Result.Success();
    }
}