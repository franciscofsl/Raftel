using System.Linq.Expressions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Raftel.Application.Contracts.Localization;
using Raftel.Application.Localization.Queries.GetList;
using Raftel.Core.Localization;

namespace Raftel.Application.Tests.Localization.Queries;

public class GetLanguagesQueryHandlerTest
{
    private readonly ILanguagesRepository _languagesRepository;
    private readonly GetLanguagesQueryHandler _handler;

    public GetLanguagesQueryHandlerTest()
    {
        _languagesRepository = Substitute.For<ILanguagesRepository>();
        _handler = new GetLanguagesQueryHandler(_languagesRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnListOfLanguageDto_WhenLanguagesExist()
    {
        var query = new GetLanguagesQuery();

        var languages = new List<Language>
        {
            Language.Create("English", "en"),
            Language.Create("Spanish", "es")
        };

        _languagesRepository.GetListAsync(Arg.Any<Expression<Func<Language, LanguageDto>>>(), null)
            .Returns(languages.ConvertAll(lang => new LanguageDto
            {
                IsoCode = lang.IsoCode,
                Name = lang.Name,
                Id = lang.Id
            }));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainSingle(x => x.IsoCode == "en" && x.Name == "English");
        result.Should().ContainSingle(x => x.IsoCode == "es" && x.Name == "Spanish");

        await _languagesRepository.Received(1).GetListAsync(Arg.Any<Expression<Func<Language, LanguageDto>>>(), null);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoLanguagesExist()
    {
        var query = new GetLanguagesQuery();
        _languagesRepository.GetListAsync(Arg.Any<Expression<Func<Language, LanguageDto>>>(), null)
            .Returns(new List<LanguageDto>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();

        await _languagesRepository.Received(1).GetListAsync(Arg.Any<Expression<Func<Language, LanguageDto>>>(), null);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenRepositoryThrowsError()
    {
        var query = new GetLanguagesQuery();
        _languagesRepository.GetListAsync(Arg.Any<Expression<Func<Language, LanguageDto>>>(), null)
            .Throws(new Exception("Database error"));

        Func<Task> action = async () => await _handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<Exception>()
            .WithMessage("Database error");

        await _languagesRepository.Received(1).GetListAsync(Arg.Any<Expression<Func<Language, LanguageDto>>>(), null);
    }
}