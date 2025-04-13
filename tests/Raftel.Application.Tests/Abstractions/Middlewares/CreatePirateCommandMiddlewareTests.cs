using Raftel.Application.Abstractions.Middlewares;
using Raftel.Application.Exceptions;
using Raftel.Application.Tests.Common;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.Tests.Abstractions.Middlewares;

public class CreatePirateCommandMiddlewareTests
{
    [Fact]
    public async Task Should_ThrowValidationException_If_CommandIsInvalid()
    {
        var validator = new CreatePirateCommandValidator();
        var middleware = new ValidationMiddleware<CreatePirateCommand, Result>([validator]);

        var invalidCommand = new CreatePirateCommand(string.Empty, 1, true);

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            middleware.HandleAsync(invalidCommand, () => Task.FromResult(Result.Success())));

        exception.Errors.ShouldContain(CreatePirateErrors.NameRequired);
        exception.Errors.ShouldContain(CreatePirateErrors.KingMustBeLuffy);
    }

    [Fact]
    public async Task Should_ContinuePipeline_If_CommandIsValid()
    {
        var validator = new CreatePirateCommandValidator();
        var middleware = new ValidationMiddleware<CreatePirateCommand, Result>([validator]);

        var validCommand = new CreatePirateCommand("Luffy", 56, true);

        var result = await middleware.HandleAsync(validCommand, () => Task.FromResult(Result.Success()));

        result.IsSuccess.ShouldBeTrue();
    }
}