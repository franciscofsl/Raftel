using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Raftel.Application.Localization.Commands.Create;
using Raftel.Core.Localization;

namespace Raftel.Application.Tests.Localization.Commands;

public class CreateLanguageCommandHandlerTests
{
    private readonly ILanguagesRepository _languagesRepository;
    private readonly CreateLanguageCommandHandler _handler;

    public CreateLanguageCommandHandlerTests()
    {
        _languagesRepository = Substitute.For<ILanguagesRepository>();
        _handler = new CreateLanguageCommandHandler(_languagesRepository);
    }

    [Fact]
    public async Task Handle_ShouldCreateLanguageAndReturnResult_WhenCommandIsValid()
    {
        var command = new CreateLanguageCommand
        {
            IsoCode = "en",
            Name = "English"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await _languagesRepository
            .Received(1)
            .InsertAsync(Arg.Any<Language>());

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.IsoCode.Should().Be("en");
        result.Value.Name.Should().Be("English");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenRepositoryThrowsException()
    {
        var command = new CreateLanguageCommand
        {
            IsoCode = "es",
            Name = "Spanish"
        };

        _languagesRepository.InsertAsync(Arg.Any<Language>()).Throws(new Exception("Database error"));

        await FluentActions.Awaiting(() => _handler.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<Exception>()
            .WithMessage("Database error");
    }
}