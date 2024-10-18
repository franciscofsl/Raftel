using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Raftel.Application.Localization.Commands.Update;
using Raftel.Core.Localization;
using Raftel.Core.Localization.ValueObjects;

namespace Raftel.Application.Tests.Localization.Commands;

public class UpdateLanguageCommandHandlerTests
{
    private readonly ILanguagesRepository _languagesRepository;
    private readonly UpdateLanguageCommandHandler _handler;

    public UpdateLanguageCommandHandlerTests()
    {
        _languagesRepository = Substitute.For<ILanguagesRepository>();
        _handler = new UpdateLanguageCommandHandler(_languagesRepository);
    }

    [Fact]
    public async Task Handle_ShouldUpdateLanguageAndReturnResult_WhenCommandIsValid()
    {
        var command = new UpdateLanguageCommand
        {
            Id = Guid.NewGuid(),
            Name = "Updated Language"
        };

        var language = Language.Create("en", "English");
        _languagesRepository.GetAsync((LanguageId)command.Id).Returns(language);

        var result = await _handler.Handle(command, CancellationToken.None);

        language.Name.Should().Be("Updated Language");
        await _languagesRepository.Received(1).UpdateAsync(language);

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(language.Id);
        result.Value.Name.Should().Be("Updated Language");
        result.Value.IsoCode.Should().Be(language.IsoCode);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenRepositoryUpdateFails()
    {
        var command = new UpdateLanguageCommand
        {
            Id = Guid.NewGuid(),
            Name = "Updated Language"
        };

        var language = Language.Create("en", "English");
        _languagesRepository.GetAsync((LanguageId)command.Id).Returns(language);

        _languagesRepository.UpdateAsync(language).Throws(new Exception("Database error"));

        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<Exception>()
            .WithMessage("Database error");
    }
}