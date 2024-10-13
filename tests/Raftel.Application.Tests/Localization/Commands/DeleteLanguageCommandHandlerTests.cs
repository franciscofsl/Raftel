using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Raftel.Application.Localization.Commands.Delete;
using Raftel.Core.BaseTypes;
using Raftel.Core.Localization;
using Raftel.Core.Localization.ValueObjects;

namespace Raftel.Application.Tests.Localization.Commands;

public class DeleteLanguageCommandHandlerTest
{
    private readonly ILanguagesRepository _languagesRepository;
    private readonly DeleteLanguageCommandHandler _handler;

    public DeleteLanguageCommandHandlerTest()
    {
        _languagesRepository = Substitute.For<ILanguagesRepository>();
        _handler = new DeleteLanguageCommandHandler(_languagesRepository);
    }

    [Fact]
    public async Task Handle_ShouldDeleteLanguage_WhenLanguageExists()
    {
        var command = new DeleteLanguageCommand(EntityIdGenerator.Create<LanguageId>());

        var language = Language.Create("en", "English");
        _languagesRepository.GetAsync(command.Id).Returns(language);

        var result = await _handler.Handle(command, CancellationToken.None);

        await _languagesRepository.Received(1).GetAsync(command.Id);
        await _languagesRepository.Received(1).DeleteAsync(language);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenRepositoryThrowsError()
    {
        var command = new DeleteLanguageCommand(EntityIdGenerator.Create<LanguageId>());

        _languagesRepository.GetAsync(command.Id).Throws(new Exception("Database error"));

        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<Exception>()
            .WithMessage("Database error");

        await _languagesRepository.DidNotReceive().DeleteAsync(Arg.Any<Language>());
    }
}