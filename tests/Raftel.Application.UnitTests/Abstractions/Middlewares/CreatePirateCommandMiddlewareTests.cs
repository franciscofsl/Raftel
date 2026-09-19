using Raftel.Application.Middlewares;
using Raftel.Demo.Application.Pirates;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Abstractions.Middlewares;

public class CreatePirateCommandMiddlewareTests
{
    [Fact]
    public async Task Should_ReturnFailedResult_If_CommandIsInvalid()
    {
        var validator = new CreatePirateCommandValidator();
        var middleware = new ValidationMiddleware<CreatePirateCommand, Result<Guid>>([validator]);

        var invalidCommand = new CreatePirateCommand(string.Empty, 1, true);

        var result = await middleware.HandleAsync(invalidCommand,
            _ => Task.FromResult(Result.Success(Guid.NewGuid())), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        var validationError = result.Error.ShouldBeOfType<ValidationError>();
        validationError.Errors.ShouldContain(CreatePirateErrors.NameRequired);
        validationError.Errors.ShouldContain(CreatePirateErrors.KingMustBeLuffy);
    }

    [Fact]
    public async Task Should_ContinuePipeline_If_CommandIsValid()
    {
        var validator = new CreatePirateCommandValidator();
        var middleware = new ValidationMiddleware<CreatePirateCommand, Result<Guid>>([validator]);

        var validCommand = new CreatePirateCommand("Luffy", 56, true);

        var result = await middleware.HandleAsync(validCommand, _ => Task.FromResult(Result.Success(Guid.NewGuid())),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }
}