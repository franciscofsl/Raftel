using NSubstitute;
using Raftel.Application.Localization.Commands.AddTranslationResource;
using Raftel.Core.BaseTypes;
using Raftel.Core.Localization;
using Raftel.Core.Localization.ValueObjects;
using Raftel.Shared.Results;

namespace Raftel.Application.Tests.Localization.Commands;

public class AddTranslationResourceCommandHandlerTest
{
    private readonly ILanguagesRepository _languagesRepository;
    private readonly AddTranslationResourceCommandHandler _handler;

    public AddTranslationResourceCommandHandlerTest()
    {
        _languagesRepository = Substitute.For<ILanguagesRepository>();
        _handler = new AddTranslationResourceCommandHandler(_languagesRepository);
    }

    [Fact]
    public async Task Handle_ShouldAddTranslationResource_WhenLanguageExists()
    {
        var command = new AddTranslationResourceCommand(EntityIdGenerator.Create<LanguageId>(), "greeting", "Hello");

        var language = Language.Create("English", "en");

        _languagesRepository.GetAsync(command.LanguageId).Returns(language);

        var result = await _handler.Handle(command, CancellationToken.None);
        result.Success.Should().BeTrue();

        await _languagesRepository.Received(1).GetAsync(command.LanguageId);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenAddingTranslationFails()
    {
        var command = new AddTranslationResourceCommand(EntityIdGenerator.Create<LanguageId>(), "greeting", "Hello");

        var language = Language.Create("English", "en");

        _languagesRepository.GetAsync(command.LanguageId).Returns(language);

        await _handler.Handle(command, CancellationToken.None);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain(LocalizationErrors.DuplicatedResource);
    }
}