using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Raftel.Application.Localization.Queries.ById;
using Raftel.Core.BaseTypes;
using Raftel.Core.Localization;
using Raftel.Core.Localization.ValueObjects;

namespace Raftel.Application.Tests.Localization.Queries;

public class GetLanguagesByIdQueryHandlerTest
{
    private readonly ILanguagesRepository _languagesRepository;
    private readonly GetLanguagesByIdQueryHandler _handler;

    public GetLanguagesByIdQueryHandlerTest()
    {
        _languagesRepository = Substitute.For<ILanguagesRepository>();
        _handler = new GetLanguagesByIdQueryHandler(_languagesRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnLanguageDto_WhenLanguageExists()
    {
        var query = new GetLanguageByIdQuery(EntityIdGenerator.Create<LanguageId>());

        var language = Language.Create("en", "English");
        _languagesRepository.GetAsync(query.Id).Returns(language);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(language.Id);
        result.Name.Should().Be(language.Name);
        result.IsoCode.Should().Be(language.IsoCode);

        await _languagesRepository.Received(1).GetAsync(query.Id);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenRepositoryThrowsError()
    {
        var query = new GetLanguageByIdQuery(EntityIdGenerator.Create<LanguageId>());

        _languagesRepository.GetAsync(query.Id).Throws(new Exception("Database error"));

        Func<Task> action = async () => await _handler.Handle(query, CancellationToken.None);

        await action
            .Should()
            .ThrowAsync<Exception>()
            .WithMessage("Database error");

        await _languagesRepository.Received(1).GetAsync(query.Id);
    }
}