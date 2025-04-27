using Raftel.Domain.Abstractions;

namespace Raftel.Api.Integration.Tests.Api.Application.Pirates.CreatePirate;

internal sealed class CreatePirateCommandHandler : ICommandHandler<CreatePirateCommand>
{
    public Task<Result> HandleAsync(CreatePirateCommand request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}